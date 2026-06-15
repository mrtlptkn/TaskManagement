using MediatR;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Services;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Features.Tasks.Commands.ChangeTaskStatus;

/// <summary>
/// ChangeTaskStatusCommand handler'ı. Action'a göre ilgili Domain metodunu çağırır.
/// Her metod kendi yetki kontrolünü (Resource Based Authorization) ve durum
/// geçiş kuralını Domain katmanında uygular; handler sadece orchestration yapar.
/// </summary>
public class ChangeTaskStatusCommandHandler : IRequestHandler<ChangeTaskStatusCommand>
{
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeTaskStatusCommandHandler(
        ITaskRepository taskRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _taskRepository = taskRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ChangeTaskStatusCommand request, CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

        var userId = _currentUserService.UserId;

        switch (request.Action)
        {
            case TaskStatusAction.StartProgress:
                task.StartProgress(userId);
                break;

            case TaskStatusAction.SubmitForReview:
                task.SubmitForReview(userId);
                break;

            case TaskStatusAction.Approve:
                task.Approve(userId);
                break;

            case TaskStatusAction.RejectReview:
                task.RejectReview(userId);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(request.Action), request.Action, "Desteklenmeyen aksiyon.");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
