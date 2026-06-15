using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions.Services;
using TaskManagement.Application.Common.Models;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Services;

/// <summary>
/// IIdentityService portunun implementasyonu.
/// UserManager, SignInManager ve IJwtTokenGenerator burada birleştirilir;
/// Application katmanı bu detayları bilmez (Hexagonal: Identity bir adapter'dır).
/// </summary>
public class IdentityService : IIdentityService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public IdentityService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<(bool Succeeded, string? UserId, IEnumerable<string> Errors)> CreateUserAsync(
        string fullName, string email, string password, string role)
    {
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
            return (false, null, new[] { "Bu e-posta adresi ile kayıtlı bir kullanıcı zaten mevcut." });

        var user = AppUser.Create(fullName, email, email);
        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
            return (false, null, result.Errors.Select(e => e.Description));

        await _userManager.AddToRoleAsync(user, role);

        return (true, user.Id.ToString(), Array.Empty<string>());
    }

    public async Task<(bool Succeeded, string? Token, DateTime ExpiresAt)> LoginAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return (false, null, default);

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (!result.Succeeded)
            return (false, null, default);

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresAt) = _jwtTokenGenerator.GenerateToken(user, roles);

        return (true, token, expiresAt);
    }

    public async Task<List<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
        => await _userManager.Users
            .AsNoTracking()
            .Select(u => new UserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty
            })
            .ToListAsync(cancellationToken);

    public async Task<string?> GetUserFullNameAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _userManager.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken);
}
