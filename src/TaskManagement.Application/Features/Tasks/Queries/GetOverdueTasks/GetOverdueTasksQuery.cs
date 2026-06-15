using MediatR;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.Features.Tasks.Queries.GetTasks;

namespace TaskManagement.Application.Features.Tasks.Queries.GetOverdueTasks;

/// <summary>
/// "Gecikmiş Görevler" için özel kısayol query'si.
/// GET /api/v1/tasks/overdue
///
/// Dahili olarak GetTasksQuery'i IsOverdue=true ile çalıştırır
/// (DRY: filtreleme mantığı tek bir yerde - TaskFilterSpecification).
/// </summary>
public record GetOverdueTasksQuery : IRequest<PagedResult<TaskListItemDto>>
{
    public Guid? AssignedToUserId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class GetOverdueTasksQueryHandler : IRequestHandler<GetOverdueTasksQuery, PagedResult<TaskListItemDto>>
{
    private readonly IMediator _mediator;

    public GetOverdueTasksQueryHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task<PagedResult<TaskListItemDto>> Handle(GetOverdueTasksQuery request, CancellationToken cancellationToken)
    {
        var query = new GetTasksQuery
        {
            AssignedToUserId = request.AssignedToUserId,
            IsOverdue = true,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        return _mediator.Send(query, cancellationToken);
    }
}
