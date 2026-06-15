using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations;

public class TaskActivityLogConfiguration : IEntityTypeConfiguration<TaskActivityLog>
{
    public void Configure(EntityTypeBuilder<TaskActivityLog> builder)
    {
        builder.ToTable("TaskActivityLogs");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Description).IsRequired().HasMaxLength(1000);
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UserId).IsRequired();
        builder.Property(a => a.TaskItemId).IsRequired();

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.TaskItemId);
    }
}
