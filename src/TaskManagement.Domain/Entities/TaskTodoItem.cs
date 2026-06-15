using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Entities;

/// <summary>
/// Bir görevin "Yapılacaklar" listesindeki tek bir checkbox öğesi.
/// Bir TaskItem'a ait tüm TaskTodoItem'lar işaretlendiğinde
/// ilgili görev otomatik olarak inceleme sürecine geçer.
/// </summary>
public class TaskTodoItem : Entity
{
    public string Title { get; private set; } = default!;
    public bool IsChecked { get; private set; }
    public Guid TaskItemId { get; private set; }

    // EF Core için parametresiz constructor
    private TaskTodoItem() { }

    private TaskTodoItem(Guid id, string title, Guid taskItemId) : base(id)
    {
        Title = title;
        IsChecked = false;
        TaskItemId = taskItemId;
    }

    public static TaskTodoItem Create(string title, Guid taskItemId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Todo başlığı boş olamaz.", nameof(title));

        return new TaskTodoItem(Guid.NewGuid(), title, taskItemId);
    }

    public void SetChecked(bool isChecked) => IsChecked = isChecked;

    public void Rename(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Todo başlığı boş olamaz.", nameof(title));

        Title = title;
    }
}
