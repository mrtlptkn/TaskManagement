using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.API.IntegrationTests;

/// <summary>
/// Integration testler için özel WebApplicationFactory.
/// SQL Server bağımlılığını ortadan kaldırmak amacıyla ApplicationDbContext'i
/// EF Core InMemory provider ile değiştirir. Bu sayede testler izole ve
/// hızlı bir şekilde, gerçek bir veritabanı gerektirmeden çalışır.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("TaskManagementTestDb");
            });
        });
    }
}
