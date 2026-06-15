using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// AppUser için ek konfigürasyon. Identity'nin temel alanları
/// IdentityDbContext tarafından zaten yapılandırılır; burada sadece
/// domain'e özel FullName alanı ve navigation backing field'lar ayarlanır.
/// </summary>
public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Navigation(u => u.CreatedTasks).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(u => u.AssignedTasks).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
