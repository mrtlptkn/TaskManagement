using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(2000);

        // Enum'lar string olarak saklanır: okunabilirlik ve migration esnekliği için.
        builder.Property(t => t.Priority)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.DeadLine).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();

        builder.Property(t => t.CreatedByUserId).IsRequired();
        builder.Property(t => t.AssignedToUserId).IsRequired();

        // CreatedByUserId -> AppUser (Restrict: kullanıcı silinirse görev silinmesin)
        builder.HasOne<AppUser>()
            .WithMany(u => u.CreatedTasks)
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // AssignedToUserId -> AppUser
        builder.HasOne<AppUser>()
            .WithMany(u => u.AssignedTasks)
            .HasForeignKey(t => t.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Aggregate içi koleksiyonlar - cascade delete (görev silinince çocukları da silinir)
        builder.HasMany(t => t.TodoItems)
            .WithOne()
            .HasForeignKey(ti => ti.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Attachments)
            .WithOne()
            .HasForeignKey(a => a.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.ActivityLogs)
            .WithOne()
            .HasForeignKey(a => a.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Private backing field'lara EF Core erişimi (encapsulation korunur).
        builder.Navigation(t => t.TodoItems).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(t => t.Attachments).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(t => t.ActivityLogs).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Filtreleme performansı için indexler
        builder.HasIndex(t => t.Priority);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.AssignedToUserId);
        builder.HasIndex(t => t.DeadLine);
    }
}
