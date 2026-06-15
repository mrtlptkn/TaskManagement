using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Exceptions;

/// <summary>
/// Tüm domain kurallarına aykırı durumlar için temel exception.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

/// <summary>
/// Geçersiz bir durum (status) geçişi denendiğinde fırlatılır.
/// Örn: Todo durumundayken direkt Completed'a geçiş.
/// </summary>
public class InvalidTaskStatusTransitionException : DomainException
{
    public TaskStatusEnum CurrentStatus { get; }
    public TaskStatusEnum TargetStatus { get; }

    public InvalidTaskStatusTransitionException(TaskStatusEnum current, TaskStatusEnum target)
        : base($"'{current}' durumundan '{target}' durumuna geçiş yapılamaz.")
    {
        CurrentStatus = current;
        TargetStatus = target;
    }
}

/// <summary>
/// Resource Based Authorization kuralı ihlal edildiğinde fırlatılır.
/// Örn: Görevi oluşturmayan birinin güncelleme/onaylama denemesi.
/// </summary>
public class UnauthorizedTaskOperationException : DomainException
{
    public UnauthorizedTaskOperationException(string message) : base(message) { }
}

/// <summary>
/// Görev üzerinde belirtilen Todo öğesi bulunamadığında fırlatılır.
/// </summary>
public class TodoItemNotFoundException : DomainException
{
    public Guid TodoItemId { get; }

    public TodoItemNotFoundException(Guid todoItemId)
        : base($"Todo item bulunamadı: {todoItemId}")
    {
        TodoItemId = todoItemId;
    }
}
