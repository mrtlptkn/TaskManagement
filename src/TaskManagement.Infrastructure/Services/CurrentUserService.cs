using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TaskManagement.Application.Abstractions.Services;

namespace TaskManagement.Infrastructure.Services;

/// <summary>
/// ICurrentUserService portunun implementasyonu.
/// JWT token'daki "sub" claim'inden mevcut kullanıcının Id'sini çözer.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

            return userIdClaim is not null && Guid.TryParse(userIdClaim, out var userId)
                ? userId
                : Guid.Empty;
        }
    }

    public ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;
}
