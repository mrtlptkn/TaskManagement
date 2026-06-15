using System.Security.Claims;
using TaskManagement.Application.Common.Models;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Abstractions.Services;

/// <summary>
/// O anki HTTP isteğini yapan kullanıcı bilgisine erişim portu.
/// JWT token'daki claim'lerden türetilir (Infrastructure'da implement edilir).
/// </summary>
public interface ICurrentUserService
{
    Guid UserId { get; }
    ClaimsPrincipal? User { get; }
}

/// <summary>
/// Görev eklerinin (attachment) fiziksel olarak saklanması için port.
/// Infrastructure'da local disk, Azure Blob Storage vb. ile implement edilebilir.
/// </summary>
public interface IFileStorageService
{
    /// <summary>Dosyayı kaydeder ve saklama yolunu (path/key) döner.</summary>
    Task<string> SaveFileAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);
}

/// <summary>
/// Identity (kullanıcı/rol) işlemleri için port.
/// UserManager/RoleManager/SignInManager kullanımını Infrastructure'a izole eder.
/// </summary>
public interface IIdentityService
{
    Task<(bool Succeeded, string? UserId, IEnumerable<string> Errors)> CreateUserAsync(
        string fullName, string email, string password, string role);

    Task<(bool Succeeded, string? Token, DateTime ExpiresAt)> LoginAsync(string email, string password);

    /// <summary>Görev atama formlarında kullanılacak kullanıcı listesini döner.</summary>
    Task<List<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<string?> GetUserFullNameAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// JWT access token üretimi için port.
/// </summary>
public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) GenerateToken(AppUser user, IEnumerable<string> roles);
}
