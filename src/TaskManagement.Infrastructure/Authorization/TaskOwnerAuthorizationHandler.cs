using Microsoft.AspNetCore.Authorization;
using TaskManagement.Application.Abstractions.Services;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Authorization;

/// <summary>
/// Resource Based Authorization gereksinimi: bir kaynağın (TaskItem) sahibi olma şartı.
/// </summary>
public class TaskOwnerRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// TaskOwnerRequirement'ın handler'ı.
/// Mevcut kullanıcı (ICurrentUserService.UserId), kaynağın CreatedByUserId'sine
/// eşitse yetkilendirme başarılı olur.
///
/// NOT: Bu projede yetki kontrolü asıl olarak Domain entity'sinin (TaskItem)
/// metodları içinde (UnauthorizedTaskOperationException ile) uygulanmaktadır
/// — bu Single Source of Truth'u garanti eder. Bu handler, IAuthorizationService
/// üzerinden ASP.NET Core'un policy-based authorization altyapısını da
/// göstermek amacıyla alternatif/ek bir katman olarak sağlanmıştır ve
/// controller seviyesinde [Authorize] policy'leriyle kullanılabilir.
/// </summary>
public class TaskOwnerAuthorizationHandler : AuthorizationHandler<TaskOwnerRequirement, TaskItem>
{
    private readonly ICurrentUserService _currentUserService;

    public TaskOwnerAuthorizationHandler(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TaskOwnerRequirement requirement,
        TaskItem resource)
    {
        if (resource.CreatedByUserId == _currentUserService.UserId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
