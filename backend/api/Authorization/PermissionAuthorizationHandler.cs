using System.Security.Claims;

using GTEK.FSM.Backend.Application.Identity;

using Microsoft.AspNetCore.Authorization;

namespace GTEK.FSM.Backend.Api.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var roles = context.User.Claims
            .Where(c => c.Type is ClaimTypes.Role or TokenClaimNames.Role or TokenClaimNames.Roles)
            .SelectMany(c => c.Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        if (RolePermissionAuthorizer.IsAuthorizedForPermission(roles, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
