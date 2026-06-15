using FluentValidation;

namespace TaskManagement.Application.Features.Tasks.Commands.ChangeTaskStatus;

public class ChangeTaskStatusCommandValidator : AbstractValidator<ChangeTaskStatusCommand>
{
    public ChangeTaskStatusCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty().WithMessage("Görev kimliği (TaskId) belirtilmelidir.");

        RuleFor(x => x.Action)
            .IsInEnum().WithMessage("Geçersiz durum geçiş aksiyonu.");
    }
}
