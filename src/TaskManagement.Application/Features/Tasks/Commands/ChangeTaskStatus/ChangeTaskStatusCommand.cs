using MediatR;

namespace TaskManagement.Application.Features.Tasks.Commands.ChangeTaskStatus;

/// <summary>
/// Görev üzerinde uygulanacak durum geçişi türü.
/// Her aksiyon, Domain'deki TaskItem metodlarına 1:1 karşılık gelir
/// ve ilgili yetki/iş kuralı kontrolleri orada uygulanır.
/// </summary>
public enum TaskStatusAction
{
    /// <summary>Todo -> InProgress (sadece atanan kullanıcı).</summary>
    StartProgress = 1,

    /// <summary>InProgress -> InReview (sadece atanan kullanıcı).</summary>
    SubmitForReview = 2,

    /// <summary>InReview -> Completed (sadece görevi oluşturan kullanıcı).</summary>
    Approve = 3,

    /// <summary>InReview -> InProgress (sadece görevi oluşturan kullanıcı).</summary>
    RejectReview = 4
}

/// <summary>
/// Görev durum geçişi komutu. Mediator üzerinden tek bir endpoint
/// (PATCH /tasks/{id}/status) ile farklı durum geçişleri tetiklenir.
/// </summary>
public record ChangeTaskStatusCommand : IRequest
{
    public Guid TaskId { get; set; }
    public TaskStatusAction Action { get; init; }
}
