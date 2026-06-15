using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Entities;

/// <summary>
/// Bir göreve eklenen dosya/dokümantasyon kaydı.
/// Fiziksel dosyanın kendisi IFileStorageService (Infrastructure adapter)
/// üzerinden saklanır; burada sadece metadata tutulur.
/// </summary>
public class TaskAttachment : Entity
{
    public string FileName { get; private set; } = default!;
    public string FilePath { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public long FileSize { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public Guid TaskItemId { get; private set; }

    private TaskAttachment() { }

    private TaskAttachment(Guid id, string fileName, string filePath, string contentType,
        long fileSize, Guid uploadedByUserId, Guid taskItemId) : base(id)
    {
        FileName = fileName;
        FilePath = filePath;
        ContentType = contentType;
        FileSize = fileSize;
        UploadedAt = DateTime.UtcNow;
        UploadedByUserId = uploadedByUserId;
        TaskItemId = taskItemId;
    }

    public static TaskAttachment Create(string fileName, string filePath, string contentType,
        long fileSize, Guid uploadedByUserId, Guid taskItemId)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Dosya adı boş olamaz.", nameof(fileName));

        if (fileSize <= 0)
            throw new ArgumentException("Dosya boyutu sıfırdan büyük olmalıdır.", nameof(fileSize));

        return new TaskAttachment(Guid.NewGuid(), fileName, filePath, contentType, fileSize,
            uploadedByUserId, taskItemId);
    }
}
