using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations;

public class TaskTodoItemConfiguration : IEntityTypeConfiguration<TaskTodoItem>
{
    public void Configure(EntityTypeBuilder<TaskTodoItem> builder)
    {
        builder.ToTable("TaskTodoItems");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(t => t.IsChecked).IsRequired();
        builder.Property(t => t.TaskItemId).IsRequired();
    }
}
