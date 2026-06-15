using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Common.Models;

/// <summary>Bir görevin tüm bilgilerini taşıyan DTO (detay görünümü).</summary>
public record TaskDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = default!;
    public string Description { get; init; } = default!;
    public TaskPriority Priority { get; init; }
    public TaskStatusEnum Status { get; init; }
    public DateTime DeadLine { get; init; }
    public bool IsOverdue { get; init; }
    public Guid CreatedByUserId { get; init; }
    public string CreatedByUserName { get; init; } = default!;
    public Guid AssignedToUserId { get; init; }
    public string AssignedToUserName { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public List<TaskTodoItemDto> TodoItems { get; init; } = new();
    public List<TaskAttachmentDto> Attachments { get; init; } = new();
    public List<TaskActivityLogDto> ActivityLogs { get; init; } = new();
}

/// <summary>Liste görünümlerinde kullanılan, daha hafif görev DTO'su.</summary>
public record TaskListItemDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = default!;
    public TaskPriority Priority { get; init; }
    public TaskStatusEnum Status { get; init; }
    public DateTime DeadLine { get; init; }
    public bool IsOverdue { get; init; }
    public string AssignedToUserName { get; init; } = default!;
    public int TotalTodoCount { get; init; }
    public int CompletedTodoCount { get; init; }
}

public class TaskTodoItemDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = default!;
    public bool IsChecked { get; init; }
}

public class TaskAttachmentDto
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = default!;
    public string ContentType { get; init; } = default!;
    public long FileSize { get; init; }
    public DateTime UploadedAt { get; init; }
}

public record TaskActivityLogDto
{
    public Guid Id { get; init; }
    public string Description { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
    public Guid UserId { get; init; }
    public string UserName { get; init; } = default!;
}

/// <summary>Görev atama için kullanıcı seçim listesi.</summary>
public class UserDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = default!;
    public string Email { get; init; } = default!;
}

public class RegisterResultDto
{
    public string UserId { get; init; } = default!;
}

public class LoginResultDto
{
    public string Token { get; init; } = default!;
    public DateTime ExpiresAt { get; init; }
}
