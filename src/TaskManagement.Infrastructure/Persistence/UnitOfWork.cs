using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Domain.Common;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Events;

namespace TaskManagement.Infrastructure.Persistence;

/// <summary>
/// SaveChangesAsync çağrıldığında önce aggregate'lerin domain event'lerini işler,
/// ActivityLog INSERT'lerini ekler, ardından tek SaveChanges'te yazar.
/// UPDATE + INSERT ayrı SQL komutları → optimistic concurrency hatası oluşmaz.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
  private readonly ApplicationDbContext _dbContext;

  public UnitOfWork(ApplicationDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
  {

    ProcessAndSaveActivityLogs();
    return await _dbContext.SaveChangesAsync(cancellationToken);

  }

  private void ProcessAndSaveActivityLogs()
  {
    var aggregates = _dbContext.ChangeTracker
        .Entries<AggregateRoot>()
        .Where(e => e.Entity.DomainEvents.Any())
        .Select(e => e.Entity)
        .ToList();

    var allEvents = aggregates.SelectMany(a => a.DomainEvents).ToList();

    aggregates.ForEach(a => a.ClearDomainEvents());

    foreach (var domainEvent in allEvents)
    {


      var (taskId, userId, message) = domainEvent switch
      {
        TaskCreatedEvent e => (e.TaskId, e.UserId, "Görev oluşturuldu."),
        TaskUpdatedEvent e => (e.TaskId, e.UserId, "Görev bilgileri güncellendi."),
        TaskStartedProgressEvent e => (e.TaskId, e.UserId, "Görev işleme alındı."),
        TaskSubmittedForReviewEvent e => (e.TaskId, e.UserId, "Görev incelemeye gönderildi."),
        TaskApprovedEvent e => (e.TaskId, e.UserId, "Görev onaylandı ve tamamlandı."),
        TaskReviewRejectedEvent e => (e.TaskId, e.UserId, "İnceleme reddedildi, görev tekrar işleme alındı."),
        TaskTodoItemAddedEvent e => (e.TaskId, e.UserId, $"Yapılacak öğe eklendi: \"{e.TodoTitle}\"."),
       
        TaskTodoItemToggledEvent e => (e.TaskId, e.UserId,
            $"\"{e.TodoTitle}\" öğesi {(e.IsChecked ? "tamamlandı olarak işaretlendi" : "işareti kaldırıldı")}."),
        TaskCommentAddedEvent e => (e.TaskId, e.UserId, e.Comment), // YENİ
        _ => (Guid.Empty, Guid.Empty, string.Empty)
      };


      if (taskId == Guid.Empty) continue;

      // 1) Önce change tracker'da ara (henüz DB'ye yazılmamış yeni TaskItem burada bulunur)
      var task = aggregates.OfType<TaskItem>().FirstOrDefault(t => t.Id == taskId)
          ?? _dbContext.ChangeTracker.Entries<TaskItem>()
              .Select(e => e.Entity)
              .FirstOrDefault(t => t.Id == taskId);

      if (task is null)
            throw new InvalidOperationException($"Activity log eklenecek TaskItem bulunamadı: {taskId}");

      task.AppendActivityLog(message, userId);


      // EF'in yeni eklenen ActivityLog'u yanlışlıkla "Modified" işaretlemesini önle.
      var newLog = task.ActivityLogs.Last();
      var entry = _dbContext.Entry(newLog);
      if (entry.State != EntityState.Added)
      {
        entry.State = EntityState.Added;
      }

    }
  }
}
