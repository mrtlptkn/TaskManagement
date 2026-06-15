using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Seed;

/// <summary>
/// Uygulama ilk ayağa kalkarken rolleri ve demo kullanıcıları oluşturur.
/// Program.cs içinde uygulama başlangıcında çağrılır.
/// </summary>
public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<AppRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
        var logger = serviceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        // Roller
        foreach (var roleName in new[] { Roles.Manager, Roles.Employee })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await roleManager.CreateAsync(new AppRole(roleName));
                if (roleResult.Succeeded)
                {
                    logger.LogInformation("Rol oluşturuldu: {RoleName}", roleName);
                }
                else
                {
                    logger.LogError("Rol oluşturulamadı: {RoleName}, Hatalar: {Errors}", 
                        roleName, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }
        }

        // Demo Manager kullanıcısı (görev oluşturan/onaylayan)
        if (await userManager.FindByEmailAsync("manager@taskmanagement.com") is null)
        {
            var manager = AppUser.Create("Ahmet Yönetici", "manager@taskmanagement.com", "manager@taskmanagement.com");
            var result = await userManager.CreateAsync(manager, "Manager123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(manager, Roles.Manager);
                logger.LogInformation("Manager kullanıcısı oluşturuldu. ID: {UserId}, Email: {Email}", 
                    manager.Id, manager.Email);
            }
            else
            {
                logger.LogError("Manager kullanıcısı oluşturulamadı. Hatalar: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        // Demo Employee kullanıcısı (göreve atanan)
        if (await userManager.FindByEmailAsync("employee@taskmanagement.com") is null)
        {
            var employee = AppUser.Create("Ayşe Çalışan", "employee@taskmanagement.com", "employee@taskmanagement.com");
            var result = await userManager.CreateAsync(employee, "Employee123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(employee, Roles.Employee);
                logger.LogInformation("Employee kullanıcısı oluşturuldu. ID: {UserId}, Email: {Email}", 
                    employee.Id, employee.Email);
            }
            else
            {
                logger.LogError("Employee kullanıcısı oluşturulamadı. Hatalar: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
