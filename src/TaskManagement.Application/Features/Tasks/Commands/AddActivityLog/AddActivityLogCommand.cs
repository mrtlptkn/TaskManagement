using FluentValidation;
using MediatR;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Services;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Features.Tasks.Commands.AddActivityLog;

/// <summary>
/// Göreve serbest metin bir akış/yorum kaydı ekler.
/// Görev üzerinde "birden fazla akış halinde giriş" gereksinimi
/// bu komut ile karşılanır (timeline / activity log).
/// </summary>
public record AddActivityLogCommand : IRequest
{
    public Guid TaskId { get; set; }
    public string Comment { get; init; } = default!;
}

public class AddActivityLogCommandValidator : AbstractValidator<AddActivityLogCommand>
{
    public AddActivityLogCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty().WithMessage("Görev kimliği (TaskId) belirtilmelidir.");

        RuleFor(x => x.Comment)
            .NotEmpty().WithMessage("Yorum boş olamaz.")
            .MaximumLength(1000).WithMessage("Yorum en fazla 1000 karakter olabilir.");
    }
}

public class AddActivityLogCommandHandler : IRequestHandler<AddActivityLogCommand>
{
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public AddActivityLogCommandHandler(
        ITaskRepository taskRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _taskRepository = taskRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AddActivityLogCommand request, CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdWithDetailsAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

        task.AddComment(request.Comment, _currentUserService.UserId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
