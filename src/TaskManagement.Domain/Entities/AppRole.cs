using Microsoft.AspNetCore.Identity;

namespace TaskManagement.Domain.Entities;

/// <summary>
/// Uygulama rolü. ASP.NET Core Identity'nin IdentityRole&lt;Guid&gt; sınıfından türetilir.
/// </summary>
public class AppRole : IdentityRole<Guid>
{
    public AppRole() { }

    public AppRole(string roleName) : base(roleName) { }
}

/// <summary>
/// Sistemde tanımlı rol isimleri için sabitler.
/// Manager: Görev oluşturabilir, atayabilir, onaylayabilir.
/// Employee: Kendisine atanan görevler üzerinde çalışabilir.
/// </summary>
public static class Roles
{
    public const string Manager = "Manager";
    public const string Employee = "Employee";
}
