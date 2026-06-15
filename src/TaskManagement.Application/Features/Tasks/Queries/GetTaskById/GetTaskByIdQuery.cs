using AutoMapper;
using FluentValidation;
using MediatR;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Services;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Models;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Features.Tasks.Queries.GetTaskById;

/// <summary>
/// Görev detayını (todo'lar, ekler ve aktivite akışı dahil) getirir.
/// </summary>
public record GetTaskByIdQuery : IRequest<TaskDto>
{
    public Guid TaskId { get; init; }
}

public class GetTaskByIdQueryValidator : AbstractValidator<GetTaskByIdQuery>
{
    public GetTaskByIdQueryValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty().WithMessage("Görev kimliği (TaskId) belirtilmelidir.");
    }
}

public class GetTaskByIdQueryHandler : IRequestHandler<GetTaskByIdQuery, TaskDto>
{
    private readonly ITaskRepository _taskRepository;
    private readonly IIdentityService _identityService;
    private readonly IMapper _mapper;

    public GetTaskByIdQueryHandler(
        ITaskRepository taskRepository,
        IIdentityService identityService,
        IMapper mapper)
    {
        _taskRepository = taskRepository;
        _identityService = identityService;
        _mapper = mapper;
    }

    public async Task<TaskDto> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdWithDetailsAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

        var dto = _mapper.Map<TaskDto>(task);

        var createdByName = await _identityService.GetUserFullNameAsync(task.CreatedByUserId, cancellationToken);
        var assignedToName = await _identityService.GetUserFullNameAsync(task.AssignedToUserId, cancellationToken);

        dto = dto with
        {
            CreatedByUserName = createdByName ?? string.Empty,
            AssignedToUserName = assignedToName ?? string.Empty,
            IsOverdue = task.IsOverdue
        };

        // ActivityLog kullanıcı adlarını doldur
        var enrichedLogs = new List<TaskActivityLogDto>();
        foreach (var log in dto.ActivityLogs)
        {
            var userName = await _identityService.GetUserFullNameAsync(log.UserId, cancellationToken);
            enrichedLogs.Add(log with { UserName = userName ?? string.Empty });
        }

        dto = dto with { ActivityLogs = enrichedLogs.OrderByDescending(l => l.CreatedAt).ToList() };

        return dto;
    }
}
