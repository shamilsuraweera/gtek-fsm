using System.Security.Claims;

using GTEK.FSM.Backend.Application.Identity;

using Microsoft.AspNetCore.Authorization;

namespace GTEK.FSM.Backend.Api.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var roles = context.User.Claims
            .Where(c => c.Type is ClaimTypes.Role or TokenClaimNames.Role or TokenClaimNames.Roles)
            .SelectMany(c => c.Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var isAuthorized = RolePermissionAuthorizer.IsAuthorizedForPermission(roles, requirement.Permission);
        
        // Extract audit context from HttpContext if available
        if (context.Resource is HttpContext httpContext)
        {
            var auditSink = httpContext.RequestServices.GetService(typeof(IAuthorizationDecisionAuditSink)) as IAuthorizationDecisionAuditSink;
            var principalAccessor = httpContext.RequestServices.GetService(typeof(IAuthenticatedPrincipalAccessor)) as IAuthenticatedPrincipalAccessor;
            var tenantAccessor = httpContext.RequestServices.GetService(typeof(ITenantContextAccessor)) as ITenantContextAccessor;
            
            if (auditSink is not null)
            {
                var principal = principalAccessor?.GetCurrent();
                var tenantId = tenantAccessor?.GetCurrentTenantId();
                
                await auditSink.WriteAsync(
                    new AuthorizationDecisionAuditEvent(
                        UserId: principal?.UserId,
                        SourceTenantId: tenantId,
                        TargetTenantId: tenantId,
                        Action: $"permission_check:{requirement.Permission}",
                        Outcome: isAuthorized ? "allowed" : "denied",
                        Reason: isAuthorized ? "permission_granted" : "permission_insufficient",
                        OccurredAtUtc: DateTimeOffset.UtcNow),
                    CancellationToken.None);
            }
        }

        if (isAuthorized)
        {
            context.Succeed(requirement);
        }
    }
}
