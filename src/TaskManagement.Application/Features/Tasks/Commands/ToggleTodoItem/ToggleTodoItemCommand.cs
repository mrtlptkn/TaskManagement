using FluentValidation;
using MediatR;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Services;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Features.Tasks.Commands.ToggleTodoItem;

/// <summary>
/// Bir görevin "Yapılacaklar" listesindeki bir öğenin checkbox durumunu değiştirir.
/// Tüm öğeler işaretlenirse, Domain katmanı görevi otomatik olarak
/// "İncelemeye Alındı" durumuna geçirir.
/// </summary>
public record ToggleTodoItemCommand : IRequest
{
    public Guid TaskId { get; set; }
    public Guid TodoItemId { get; set; }
    public bool IsChecked { get; init; }
}

public class ToggleTodoItemCommandValidator : AbstractValidator<ToggleTodoItemCommand>
{
    public ToggleTodoItemCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty().WithMessage("Görev kimliği (TaskId) belirtilmelidir.");
        RuleFor(x => x.TodoItemId).NotEmpty().WithMessage("Todo kimliği (TodoItemId) belirtilmelidir.");
    }
}

public class ToggleTodoItemCommandHandler : IRequestHandler<ToggleTodoItemCommand>
{
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public ToggleTodoItemCommandHandler(
        ITaskRepository taskRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _taskRepository = taskRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ToggleTodoItemCommand request, CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdWithDetailsAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

        task.ToggleTodoItem(request.TodoItemId, request.IsChecked, _currentUserService.UserId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
