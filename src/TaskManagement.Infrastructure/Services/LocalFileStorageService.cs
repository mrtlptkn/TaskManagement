using TaskManagement.Application.Abstractions.Services;

namespace TaskManagement.Infrastructure.Services;

/// <summary>
/// IFileStorageService portunun local disk implementasyonu.
/// Eğitim/demo amaçlıdır; production'da Azure Blob Storage, S3 vb.
/// ile değiştirilmesi için sadece bu sınıfın yeniden implement edilmesi
/// ve DI kaydının güncellenmesi yeterlidir (Open/Closed Principle).
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storageRootPath;

    public LocalFileStorageService(string? storageRootPath = null)
    {
        _storageRootPath = storageRootPath
            ?? Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "Attachments");

        Directory.CreateDirectory(_storageRootPath);
    }

    public async Task<string> SaveFileAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var safeFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var fullPath = Path.Combine(_storageRootPath, safeFileName);

        using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(fileStream, cancellationToken);

        return fullPath;
    }
}
