using FluentValidation;
using MediatR;
using TaskManagement.Application.Abstractions.Services;
using TaskManagement.Application.Common.Models;

namespace TaskManagement.Application.Features.Auth.Commands.Login;

/// <summary>
/// Kullanıcı girişi komutu. Başarılı girişte JWT access token döner.
/// </summary>
public record LoginCommand : IRequest<LoginResultDto>
{
    public string Email { get; init; } = default!;
    public string Password { get; init; } = default!;
}

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta boş olamaz.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre boş olamaz.");
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResultDto>
{
    private readonly IIdentityService _identityService;

    public LoginCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<LoginResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var (succeeded, token, expiresAt) = await _identityService.LoginAsync(request.Email, request.Password);

        if (!succeeded)
            throw new UnauthorizedAccessException("E-posta veya şifre hatalı.");

        return new LoginResultDto { Token = token!, ExpiresAt = expiresAt };
    }
}
