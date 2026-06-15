using MediatR;
using TaskManagement.Application.Abstractions.Services;
using TaskManagement.Application.Common.Models;

namespace TaskManagement.Application.Features.Users.Queries.GetUsers;

/// <summary>
/// Görev atama formlarında kullanılacak kullanıcı listesini döner.
/// GET /api/v1/users
/// </summary>
public record GetUsersQuery : IRequest<List<UserDto>>;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, List<UserDto>>
{
    private readonly IIdentityService _identityService;

    public GetUsersQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<List<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        => _identityService.GetUsersAsync(cancellationToken);
}
