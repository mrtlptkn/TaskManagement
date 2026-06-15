using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence;

/// <summary>
/// Uygulamanın EF Core DbContext'i.
/// IdentityDbContext&lt;AppUser, AppRole, Guid&gt;'den türeyerek Identity tablolarını
/// (AspNetUsers, AspNetRoles, AspNetUserRoles vb. - burada Users/Roles olarak yeniden adlandırıldı)
/// otomatik olarak modele dahil eder.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<AppUser, AppRole, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<TaskTodoItem> TaskTodoItems => Set<TaskTodoItem>();
    public DbSet<TaskAttachment> TaskAttachments => Set<TaskAttachment>();
    public DbSet<TaskActivityLog> TaskActivityLogs => Set<TaskActivityLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Identity tablolarının (AspNetUsers, AspNetRoles, AspNetUserRoles vb.) model konfigürasyonu.
        base.OnModelCreating(builder);

        // Bu assembly içindeki tüm IEntityTypeConfiguration<T> implementasyonlarını uygula.
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Identity tablo adlarını sadeleştir (opsiyonel, okunabilirlik için).
        builder.Entity<AppUser>().ToTable("Users");
        builder.Entity<AppRole>().ToTable("Roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>>().ToTable("UserTokens");
    }
}
