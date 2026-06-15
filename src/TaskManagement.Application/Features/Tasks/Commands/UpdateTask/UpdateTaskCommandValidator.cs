using FluentValidation;

namespace TaskManagement.Application.Features.Tasks.Commands.UpdateTask;

public class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty().WithMessage("Görev kimliği (TaskId) belirtilmelidir.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Görev başlığı boş olamaz.")
            .MaximumLength(200).WithMessage("Görev başlığı en fazla 200 karakter olabilir.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Açıklama en fazla 2000 karakter olabilir.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Geçersiz öncelik değeri.");

        RuleFor(x => x.DeadLine)
            .GreaterThan(DateTime.UtcNow).WithMessage("Son tarih (deadline) gelecekte bir tarih olmalıdır.");

        RuleFor(x => x.AssignedToUserId)
            .NotEmpty().WithMessage("Görev bir kullanıcıya atanmalıdır.");
    }
}
