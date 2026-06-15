using MediatR;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Features.Tasks.Commands.UpdateTask;

/// <summary>
/// Görev güncelleme komutu.
/// Resource Based Authorization: yalnızca görevi oluşturan kullanıcı (CreatedByUserId)
/// bu işlemi gerçekleştirebilir. Yetki kontrolü TaskItem.Update metodunda yapılır.
/// </summary>
public record UpdateTaskCommand : IRequest
{
    public Guid TaskId { get; set; }
    public string Title { get; init; } = default!;
    public string Description { get; init; } = default!;
    public TaskPriority Priority { get; init; }
    public DateTime DeadLine { get; init; }
    public Guid AssignedToUserId { get; init; }
}
