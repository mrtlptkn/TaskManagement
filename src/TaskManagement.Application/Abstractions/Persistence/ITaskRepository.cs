using TaskManagement.Application.Common.Specifications;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Abstractions.Persistence;

/// <summary>
/// TaskItem aggregate'i için repository portu (outbound port).
/// Infrastructure katmanında EF Core ile implement edilir.
/// </summary>
public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Todo, Attachment ve ActivityLog koleksiyonlarını da yükler (detay görünümü için).</summary>
    Task<TaskItem?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(List<TaskItem> Items, int TotalCount)> GetFilteredAsync(
        TaskFilterSpecification specification,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(TaskItem task, CancellationToken cancellationToken = default);

    void Remove(TaskItem task);
}
