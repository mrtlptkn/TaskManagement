using MediatR;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Features.Tasks.Commands.CreateTask;

/// <summary>
/// Yeni bir görev oluşturma komutu. Oluşturan kullanıcı (CreatedByUserId)
/// ICurrentUserService üzerinden handler içinde çözülür; client tarafından
/// gönderilmez (güvenlik: kullanıcı kendi adına işlem yapamaz başkası adına yapamaz).
/// </summary>
public record CreateTaskCommand : IRequest<Guid>
{
    public string Title { get; init; } = default!;
    public string Description { get; init; } = default!;
    public TaskPriority Priority { get; init; }
    public DateTime DeadLine { get; init; }
    public Guid AssignedToUserId { get; init; }

    /// <summary>Görev oluşturulurken eklenecek opsiyonel "Yapılacaklar" başlıkları.</summary>
    public List<string> TodoItems { get; init; } = new();
}
