using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Common.Specifications;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// ITaskRepository portunun EF Core implementasyonu (adapter).
/// Specification pattern üzerinden gelen Expression kriterleri
/// IQueryable.Where ile sırayla uygulanır.
/// </summary>
public class TaskRepository : ITaskRepository
{
    private readonly ApplicationDbContext _dbContext;

    public TaskRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tasks
            .Include(t => t.TodoItems)
            .Include(t => t.Attachments)
            .Include(t => t.ActivityLogs)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }



    public Task<TaskItem?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Tasks
            .Include(t => t.TodoItems)
            .Include(t => t.Attachments)
            .Include(t => t.ActivityLogs)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<(List<TaskItem> Items, int TotalCount)> GetFilteredAsync(
        TaskFilterSpecification specification,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TaskItem> query = _dbContext.Tasks
            .Include(t => t.TodoItems)
            .AsNoTracking();

        foreach (var criteria in specification.Criteria)
        {
            query = query.Where(criteria);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.DeadLine)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        _dbContext.Tasks.Add(task);
        return Task.CompletedTask;
    }

    public void Remove(TaskItem task) => _dbContext.Tasks.Remove(task);
}
