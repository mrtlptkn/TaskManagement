using FluentValidation;
using FluentValidation.Results;
using MediatR;
using TaskManagement.Application.Abstractions.Services;
using TaskManagement.Application.Common.Models;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Features.Auth.Commands.Register;

/// <summary>
/// Kullanıcı kayıt komutu. ASP.NET Core Identity (UserManager) üzerinden
/// kullanıcı oluşturur ve belirtilen role atar.
/// </summary>
public record RegisterCommand : IRequest<RegisterResultDto>
{
    public string FullName { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string Password { get; init; } = default!;

    /// <summary>Manager veya Employee. Bkz. Domain.Entities.Roles.</summary>
    public string Role { get; init; } = Roles.Employee;
}

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Ad Soyad boş olamaz.")
            .MaximumLength(150);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta boş olamaz.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre boş olamaz.")
            .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır.");

        RuleFor(x => x.Role)
            .Must(r => r == Roles.Manager || r == Roles.Employee)
            .WithMessage($"Rol '{Roles.Manager}' veya '{Roles.Employee}' olmalıdır.");
    }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResultDto>
{
    private readonly IIdentityService _identityService;

    public RegisterCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<RegisterResultDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var (succeeded, userId, errors) = await _identityService.CreateUserAsync(
            request.FullName, request.Email, request.Password, request.Role);

        if (!succeeded)
        {
            var failures = errors.Select(e => new ValidationFailure(string.Empty, e));
            throw new ValidationException(failures);
        }

        return new RegisterResultDto { UserId = userId! };
    }
}
