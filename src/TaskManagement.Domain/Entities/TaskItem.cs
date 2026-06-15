using TaskManagement.Domain.Common;
using TaskManagement.Domain.Common;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Events;
using TaskManagement.Domain.Exceptions;

namespace TaskManagement.Domain.Entities;

/// <summary>
/// Görev (Task) aggregate root'u.
/// Domain event fırlatarak activity log'u SaveChanges öncesinde
/// UnitOfWork tarafından ayrı INSERT olarak işler.
/// Bu sayede UPDATE + INSERT aynı SaveChanges'te çakışmaz.
/// </summary>
public class TaskItem : AggregateRoot
{
  public string Title { get; private set; } = default!;
  public string Description { get; private set; } = default!;
  public TaskPriority Priority { get; private set; }
  public TaskStatusEnum Status { get; private set; }
  public DateTime DeadLine { get; private set; }

  /// <summary>Görevi oluşturan (yetkili) hesap. Sadece bu kullanıcı Update/Approve yapabilir.</summary>
  public Guid CreatedByUserId { get; private set; }

  /// <summary>Görevin atandığı çalışan.</summary>
  public Guid AssignedToUserId { get; private set; }

  private readonly List<TaskTodoItem> _todoItems = new();
  public IReadOnlyCollection<TaskTodoItem> TodoItems => _todoItems.AsReadOnly();

  private readonly List<TaskAttachment> _attachments = new();
  public IReadOnlyCollection<TaskAttachment> Attachments => _attachments.AsReadOnly();

  private readonly List<TaskActivityLog> _activityLogs = new();
  public IReadOnlyCollection<TaskActivityLog> ActivityLogs => _activityLogs.AsReadOnly();

  /// <summary>Görev tamamlanmamış ve son tarihi geçmişse true döner.</summary>
  public bool IsOverdue => DeadLine < DateTime.UtcNow && Status != TaskStatusEnum.Completed;

  // EF Core için parametresiz constructor
  private TaskItem() { }

  private TaskItem(Guid id, string title, string description, TaskPriority priority,
      DateTime deadLine, Guid createdByUserId, Guid assignedToUserId) : base(id)
  {
    Title = title;
    Description = description;
    Priority = priority;
    DeadLine = deadLine;
    Status = TaskStatusEnum.Todo;
    CreatedByUserId = createdByUserId;
    AssignedToUserId = assignedToUserId;
    CreatedAt = DateTime.UtcNow;
  }

  /// <summary>
  /// Yeni bir görev oluşturur. Başlangıç durumu her zaman Todo'dur.
  /// </summary>
  public static TaskItem Create(string title, string description, TaskPriority priority,
      DateTime deadLine, Guid createdByUserId, Guid assignedToUserId)
  {
    if (string.IsNullOrWhiteSpace(title))
      throw new ArgumentException("Görev başlığı boş olamaz.", nameof(title));

    if (deadLine <= DateTime.UtcNow)
      throw new ArgumentException("Görev son tarihi gelecekte bir tarih olmalıdır.", nameof(deadLine));

    if (createdByUserId == Guid.Empty)
      throw new ArgumentException("Oluşturan kullanıcı belirtilmelidir.", nameof(createdByUserId));

    if (assignedToUserId == Guid.Empty)
      throw new ArgumentException("Görev bir kullanıcıya atanmalıdır.", nameof(assignedToUserId));

    var task = new TaskItem(Guid.NewGuid(), title, description ?? string.Empty, priority,
        deadLine, createdByUserId, assignedToUserId);

    task.RaiseDomainEvent(new TaskCreatedEvent(task.Id, createdByUserId));
    return task;
  }

  /// <summary>
  /// Görevi oluşturan kullanıcı tarafından genel bilgilerin güncellenmesi.
  /// Resource Based Authorization: requestUserId == CreatedByUserId olmalıdır.
  /// </summary>
  public void Update(string title, string description, TaskPriority priority,
      DateTime deadLine, Guid assignedToUserId, Guid requestUserId)
  {
    EnsureRequesterIsCreator(requestUserId, "Sadece görevi oluşturan kullanıcı güncelleyebilir.");

    if (string.IsNullOrWhiteSpace(title))
      throw new ArgumentException("Görev başlığı boş olamaz.", nameof(title));


    Title = title;
    Description = description ?? string.Empty;
    Priority = priority;
    DeadLine = deadLine;
    AssignedToUserId = assignedToUserId;
    SetUpdated();

    RaiseDomainEvent(new TaskUpdatedEvent(Id, requestUserId));
  }

  /// <summary>
  /// Görev, atanan kişi tarafından işleme alınır: Todo -> InProgress.
  /// </summary>
  public void StartProgress(Guid requestUserId)
  {
    EnsureRequesterIsAssignee(requestUserId, "Sadece atanan kullanıcı görevi işleme alabilir.");

    if (Status != TaskStatusEnum.Todo)
      throw new InvalidTaskStatusTransitionException(Status, TaskStatusEnum.InProgress);

    Status = TaskStatusEnum.InProgress;
    SetUpdated();

    RaiseDomainEvent(new TaskStartedProgressEvent(Id, requestUserId));
  }

  /// <summary>
  /// Atanan kişi görevi manuel olarak incelemeye gönderir: InProgress -> InReview.
  /// </summary>
  public void SubmitForReview(Guid requestUserId)
  {
    EnsureRequesterIsAssignee(requestUserId, "Sadece atanan kullanıcı görevi incelemeye gönderebilir.");

    if (Status != TaskStatusEnum.InProgress)
      throw new InvalidTaskStatusTransitionException(Status, TaskStatusEnum.InReview);

    Status = TaskStatusEnum.InReview;
    SetUpdated();

    RaiseDomainEvent(new TaskSubmittedForReviewEvent(Id, requestUserId));
  }

  /// <summary>
  /// Görevi oluşturan kullanıcı incelemeyi onaylar: InReview -> Completed.
  /// Resource Based Authorization: requestUserId == CreatedByUserId olmalıdır.
  /// </summary>
  public void Approve(Guid requestUserId)
  {
    EnsureRequesterIsCreator(requestUserId, "Sadece görevi oluşturan kullanıcı onaylayabilir.");

    if (Status != TaskStatusEnum.InReview)
      throw new InvalidTaskStatusTransitionException(Status, TaskStatusEnum.Completed);

    Status = TaskStatusEnum.Completed;
    SetUpdated();

    RaiseDomainEvent(new TaskApprovedEvent(Id, requestUserId));
  }

  /// <summary>
  /// Görevi oluşturan kullanıcı incelemeyi reddeder: InReview -> InProgress.
  /// Böylece atanan kişi tekrar üzerinde çalışabilir.
  /// </summary>
  public void RejectReview(Guid requestUserId)
  {
    EnsureRequesterIsCreator(requestUserId, "Sadece görevi oluşturan kullanıcı incelemeyi reddedebilir.");

    if (Status != TaskStatusEnum.InReview)
      throw new InvalidTaskStatusTransitionException(Status, TaskStatusEnum.InProgress);

    Status = TaskStatusEnum.InProgress;
    SetUpdated();

    RaiseDomainEvent(new TaskReviewRejectedEvent(Id, requestUserId));
  }

  /// <summary>
  /// Yeni bir "Yapılacaklar" (todo/checkbox) öğesi ekler.
  /// </summary>
  public void AddTodoItem(string title, Guid requestUserId)
  {
    var todo = TaskTodoItem.Create(title, Id);
    _todoItems.Add(todo);
    SetUpdated();

    RaiseDomainEvent(new TaskTodoItemAddedEvent(Id, title, requestUserId));
  }

  /// <summary>
  /// Bir todo öğesinin checkbox durumunu değiştirir.
  /// Tüm todo'lar işaretlenirse ve görev InProgress durumundaysa,
  /// görev otomatik olarak InReview durumuna geçer.
  /// </summary>
  public void ToggleTodoItem(Guid todoItemId, bool isChecked, Guid requestUserId)
  {
    EnsureRequesterIsAssignee(requestUserId, "Sadece atanan kullanıcı yapılacaklar listesini güncelleyebilir.");

    var todo = _todoItems.FirstOrDefault(t => t.Id == todoItemId)
        ?? throw new TodoItemNotFoundException(todoItemId);

    todo.SetChecked(isChecked);
    SetUpdated();

    RaiseDomainEvent(new TaskTodoItemToggledEvent(Id, todo.Title, isChecked, requestUserId));

    if (_todoItems.Count > 0 && _todoItems.All(t => t.IsChecked) && Status == TaskStatusEnum.InProgress)
      Status = TaskStatusEnum.InReview;
  }

  /// <summary>
  /// Göreve dosya eki ekler (metadata kaydı; fiziksel dosya storage adapter'da tutulur).
  /// </summary>
  public void AddAttachment(string fileName, string filePath, string contentType, long fileSize, Guid requestUserId)
  {
    var attachment = TaskAttachment.Create(fileName, filePath, contentType, fileSize, requestUserId, Id);
    _attachments.Add(attachment);
    SetUpdated();
  }

  /// <summary>
  /// Göreve serbest metin bir aktivite/yorum kaydı ekler.
  /// Görev üzerinde "birden fazla akış" (timeline) bu şekilde temsil edilir.
  /// </summary>
  public void AddComment(string comment, Guid requestUserId)
  {
    if (string.IsNullOrWhiteSpace(comment))
      throw new ArgumentException("Yorum boş olamaz.", nameof(comment));

    SetUpdated();
    RaiseDomainEvent(new TaskCommentAddedEvent(Id, requestUserId, comment));
  }

  /// <summary>UnitOfWork tarafından domain event işlendikten sonra çağrılır.</summary>
  public void AppendActivityLog(string description, Guid userId)
      => _activityLogs.Add(TaskActivityLog.Create(description, userId, Id));

  private void EnsureRequesterIsCreator(Guid requestUserId, string errorMessage)
  {
    if (requestUserId != CreatedByUserId)
      throw new UnauthorizedTaskOperationException(errorMessage);
  }

  private void EnsureRequesterIsAssignee(Guid requestUserId, string errorMessage)
  {
    if (requestUserId != AssignedToUserId)
      throw new UnauthorizedTaskOperationException(errorMessage);
  }
}
