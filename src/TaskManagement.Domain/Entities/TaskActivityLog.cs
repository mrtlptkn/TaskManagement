using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Entities;

/// <summary>
/// Bir görevle ilgili kronolojik akış/aktivite kaydı.
/// Görev üzerinde yapılan her önemli işlem (oluşturma, durum değişikliği,
/// todo işaretleme, yorum, ek dosya yükleme vb.) burada loglanır.
/// Bu sayede bir görev üzerinde "birden fazla akış" (timeline) tutulabilir.
/// </summary>
public class TaskActivityLog : Entity
{
    public string Description { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public Guid UserId { get; private set; }
    public Guid TaskItemId { get; private set; }

    private TaskActivityLog() { }

    private TaskActivityLog(Guid id, string description, Guid userId, Guid taskItemId) : base(id)
    {
        Description = description;
        CreatedAt = DateTime.UtcNow;
        UserId = userId;
        TaskItemId = taskItemId;
    }

    public static TaskActivityLog Create(string description, Guid userId, Guid taskItemId)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Aktivite açıklaması boş olamaz.", nameof(description));

        return new TaskActivityLog(Guid.NewGuid(), description, userId, taskItemId);
    }
}
