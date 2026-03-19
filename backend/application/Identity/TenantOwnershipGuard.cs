namespace GTEK.FSM.Backend.Application.Identity;

public sealed class TenantOwnershipGuard(
    IAuthenticatedPrincipalAccessor principalAccessor,
    ITenantContextAccessor tenantContextAccessor) : ITenantOwnershipGuard
{
    public TenantOwnershipGuardResult EnsureTenantAccess(Guid requestedTenantId)
    {
        if (requestedTenantId == Guid.Empty)
        {
            return TenantOwnershipGuardResult.Reject(
                statusCode: 400,
                errorCode: "INVALID_TENANT_ID",
                message: "Requested tenant id must be a non-empty GUID.");
        }

        var principal = principalAccessor.GetCurrent();
        if (principal is null)
        {
            return TenantOwnershipGuardResult.Reject(
                statusCode: 401,
                errorCode: "AUTH_UNAUTHORIZED",
                message: "Authentication is required.");
        }

        var resolvedTenant = tenantContextAccessor.GetCurrentTenantId();
        if (!resolvedTenant.HasValue)
        {
            return TenantOwnershipGuardResult.Reject(
                statusCode: 401,
                errorCode: "TENANT_CONTEXT_UNRESOLVED",
                message: "Tenant context is required.");
        }

        if (principal.TenantId != requestedTenantId)
        {
            return TenantOwnershipGuardResult.Reject(
                statusCode: 403,
                errorCode: "TENANT_OWNERSHIP_MISMATCH",
                message: "Requested tenant does not match authenticated principal tenant.");
        }

        if (resolvedTenant.Value != requestedTenantId)
        {
            return TenantOwnershipGuardResult.Reject(
                statusCode: 403,
                errorCode: "TENANT_CONTEXT_MISMATCH",
                message: "Requested tenant does not match resolved tenant context.");
        }

        return TenantOwnershipGuardResult.Allow();
    }
}
