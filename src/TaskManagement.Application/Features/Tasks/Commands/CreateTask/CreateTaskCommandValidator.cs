using FluentValidation;
using Microsoft.AspNetCore.Identity;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Features.Tasks.Commands.CreateTask;

public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    private readonly UserManager<AppUser> _userManager;

    public CreateTaskCommandValidator(UserManager<AppUser> userManager)
    {
        _userManager = userManager;

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
            .NotEmpty().WithMessage("Görev bir kullanıcıya atanmalıdır.")
            .MustAsync(UserExists).WithMessage("Atanan kullanıcı bulunamadı.");

        RuleForEach(x => x.TodoItems)
            .NotEmpty().WithMessage("Yapılacaklar öğesi boş olamaz.")
            .MaximumLength(300).WithMessage("Yapılacaklar öğesi en fazla 300 karakter olabilir.");
    }

    private async Task<bool> UserExists(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user is not null;
    }
}
