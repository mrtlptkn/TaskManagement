namespace TaskManagement.Infrastructure.Services;

/// <summary>
/// appsettings.json -> "JwtSettings" bölümünden bind edilen ayarlar.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string SecretKey { get; init; } = default!;
    public string Issuer { get; init; } = default!;
    public string Audience { get; init; } = default!;
    public int ExpiryMinutes { get; init; } = 60;
}
