using AutoMapper;
using FluentValidation;
using MediatR;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Services;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.Common.Specifications;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Features.Tasks.Queries.GetTasks;

/// <summary>
/// Görevleri filtreli ve sayfalı şekilde listeler.
///
/// Desteklenen sorgu parametreleri:
///  - Priority         : Önceliğe göre filtreleme
///  - Status           : Duruma göre filtreleme
///  - AssignedToUserId : Atanan kullanıcıya göre filtreleme
///  - IsOverdue        : Sadece gecikmiş görevler
///  - DueWithinWeek    : Son tarihi 1 hafta içinde olan görevler
///
/// Örnek: GET /api/v1/tasks?priority=Urgent&amp;isOverdue=true&amp;pageNumber=1&amp;pageSize=20
/// </summary>
public record GetTasksQuery : IRequest<PagedResult<TaskListItemDto>>
{
    public TaskPriority? Priority { get; init; }
    public TaskStatusEnum? Status { get; init; }
    public Guid? AssignedToUserId { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public bool? IsOverdue { get; init; }
    public bool? DueWithinWeek { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class GetTasksQueryValidator : AbstractValidator<GetTasksQuery>
{
    public GetTasksQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1).WithMessage("PageNumber 1 veya daha büyük olmalıdır.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("PageSize 1 ile 100 arasında olmalıdır.");

        When(x => x.Priority.HasValue, () =>
            RuleFor(x => x.Priority!.Value).IsInEnum().WithMessage("Geçersiz öncelik değeri."));

        When(x => x.Status.HasValue, () =>
            RuleFor(x => x.Status!.Value).IsInEnum().WithMessage("Geçersiz durum değeri."));
    }
}

public class GetTasksQueryHandler : IRequestHandler<GetTasksQuery, PagedResult<TaskListItemDto>>
{
    private readonly ITaskRepository _taskRepository;
    private readonly IIdentityService _identityService;
    private readonly IMapper _mapper;

    public GetTasksQueryHandler(ITaskRepository taskRepository, IIdentityService identityService, IMapper mapper)
    {
        _taskRepository = taskRepository;
        _identityService = identityService;
        _mapper = mapper;
    }

    public async Task<PagedResult<TaskListItemDto>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
    {
        var specification = new TaskFilterSpecification(
            priority: request.Priority,
            status: request.Status,
            assignedToUserId: request.AssignedToUserId,
            createdByUserId: request.CreatedByUserId,
            isOverdue: request.IsOverdue,
            dueWithinWeek: request.DueWithinWeek);

        var (items, totalCount) = await _taskRepository.GetFilteredAsync(
            specification, request.PageNumber, request.PageSize, cancellationToken);

        var dtos = new List<TaskListItemDto>(items.Count);
        var userNameCache = new Dictionary<Guid, string>();

        foreach (var task in items)
        {
            if (!userNameCache.TryGetValue(task.AssignedToUserId, out var assignedToName))
            {
                assignedToName = await _identityService.GetUserFullNameAsync(task.AssignedToUserId, cancellationToken) ?? string.Empty;
                userNameCache[task.AssignedToUserId] = assignedToName;
            }

            var dto = _mapper.Map<TaskListItemDto>(task);
            dtos.Add(dto with { AssignedToUserName = assignedToName, IsOverdue = task.IsOverdue });
        }

        return new PagedResult<TaskListItemDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
