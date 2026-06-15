using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Common.Specifications;

/// <summary>
/// Görev listeleme/filtreleme endpoint'i için kullanılan specification.
///
/// Desteklenen filtreler:
///  - Priority        : Belirli bir önceliğe göre filtreleme
///  - Status          : Duruma göre filtreleme
///  - AssignedToUserId: Atanan kullanıcıya göre filtreleme
///  - IsOverdue       : Deadline'ı geçmiş ve tamamlanmamış görevler
///  - DueWithinWeek   : Son 1 hafta içinde teslim tarihi olan görevler
/// </summary>
public class TaskFilterSpecification : Specification<TaskItem>
{
    public TaskFilterSpecification(
        TaskPriority? priority = null,
        TaskStatusEnum? status = null,
        Guid? assignedToUserId = null,
        Guid? createdByUserId = null,
        bool? isOverdue = null,
        bool? dueWithinWeek = null)
    {
        if (priority.HasValue)
            AddCriteria(t => t.Priority == priority.Value);

        if (status.HasValue)
            AddCriteria(t => t.Status == status.Value);

        if (assignedToUserId.HasValue)
            AddCriteria(t => t.AssignedToUserId == assignedToUserId.Value);

        if (createdByUserId.HasValue)
            AddCriteria(t => t.CreatedByUserId == createdByUserId.Value);

        if (isOverdue == true)
        {
            var now = DateTime.UtcNow;
            AddCriteria(t => t.DeadLine < now && t.Status != TaskStatusEnum.Completed);
        }

        if (dueWithinWeek == true)
        {
            var now = DateTime.UtcNow;
            var weekLater = now.AddDays(7);
            AddCriteria(t => t.DeadLine >= now && t.DeadLine <= weekLater);
        }
    }
}
