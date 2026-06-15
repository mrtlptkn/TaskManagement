using Microsoft.AspNetCore.Identity;

namespace TaskManagement.Domain.Entities;

/// <summary>
/// Uygulama kullanıcısı. ASP.NET Core Identity'nin IdentityUser&lt;Guid&gt; sınıfından türetilir;
/// bu sayede Email, UserName, PasswordHash, SecurityStamp gibi alanlar Identity tarafından
/// otomatik sağlanır ve UserManager/SignInManager ile yönetilir.
///
/// Domain'e özel olarak yalnızca FullName eklenmiştir.
/// </summary>
public class AppUser : IdentityUser<Guid>
{
    public string FullName { get; private set; } = default!;

    /// <summary>Bu kullanıcının oluşturduğu görevler (CreatedByUserId FK).</summary>
    private readonly List<TaskItem> _createdTasks = new();
    public IReadOnlyCollection<TaskItem> CreatedTasks => _createdTasks.AsReadOnly();

    /// <summary>Bu kullanıcıya atanan görevler (AssignedToUserId FK).</summary>
    private readonly List<TaskItem> _assignedTasks = new();
    public IReadOnlyCollection<TaskItem> AssignedTasks => _assignedTasks.AsReadOnly();

    // EF Core / Identity için parametresiz constructor gereklidir.
    public AppUser() { }

    public static AppUser Create(string fullName, string email, string userName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Ad Soyad boş olamaz.", nameof(fullName));

        return new AppUser
        {
            // Id manuel olarak atanmaz, Identity otomatik atar
            FullName = fullName,
            Email = email,
            UserName = userName,
            // Eğitim/demo amaçlı; production'da e-posta doğrulama akışı kurulmalıdır.
            EmailConfirmed = true
        };
    }

    public void UpdateFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Ad Soyad boş olamaz.", nameof(fullName));

        FullName = fullName;
    }
}
