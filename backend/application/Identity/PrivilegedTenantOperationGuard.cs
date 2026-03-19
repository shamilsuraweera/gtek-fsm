namespace GTEK.FSM.Backend.Application.Identity;

public sealed class PrivilegedTenantOperationGuard(
    IAuthenticatedPrincipalAccessor principalAccessor,
    ITenantContextAccessor tenantContextAccessor,
    IAuthorizationDecisionAuditSink auditSink) : IPrivilegedTenantOperationGuard
{
    public async Task<PrivilegedTenantOperationGuardResult> EvaluateAsync(
        PrivilegedTenantOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.TargetTenantId == Guid.Empty)
        {
            return await RejectAsync(
                userId: null,
                sourceTenantId: tenantContextAccessor.GetCurrentTenantId(),
                targetTenantId: request.TargetTenantId,
                action: request.Action,
                statusCode: 400,
                errorCode: "INVALID_TARGET_TENANT",
                message: "Target tenant id must be a non-empty GUID.",
                cancellationToken);
        }

        var principal = principalAccessor.GetCurrent();
        if (principal is null)
        {
            return await RejectAsync(
                userId: null,
                sourceTenantId: tenantContextAccessor.GetCurrentTenantId(),
                targetTenantId: request.TargetTenantId,
                action: request.Action,
                statusCode: 401,
                errorCode: "AUTH_UNAUTHORIZED",
                message: "Authentication is required.",
                cancellationToken);
        }

        var sourceTenantId = tenantContextAccessor.GetCurrentTenantId();
        if (!sourceTenantId.HasValue)
        {
            return await RejectAsync(
                userId: principal.UserId,
                sourceTenantId: null,
                targetTenantId: request.TargetTenantId,
                action: request.Action,
                statusCode: 401,
                errorCode: "TENANT_CONTEXT_UNRESOLVED",
                message: "Tenant context is required.",
                cancellationToken);
        }

        var isCrossTenant = sourceTenantId.Value != request.TargetTenantId;
        if (isCrossTenant && !RolePermissionAuthorizer.IsAuthorizedForPermission(principal.Roles, Permissions.TenantsWrite))
        {
            return await RejectAsync(
                userId: principal.UserId,
                sourceTenantId: sourceTenantId,
                targetTenantId: request.TargetTenantId,
                action: request.Action,
                statusCode: 403,
                errorCode: "CROSS_TENANT_FORBIDDEN",
                message: "Cross-tenant management flow requires privileged tenant-write permission.",
                cancellationToken);
        }

        await auditSink.WriteAsync(
            new AuthorizationDecisionAuditEvent(
                UserId: principal.UserId,
                SourceTenantId: sourceTenantId,
                TargetTenantId: request.TargetTenantId,
                Action: request.Action,
                Outcome: "allowed",
                Reason: isCrossTenant
                    ? "privileged_cross_tenant_operation_allowed"
                    : "same_tenant_operation_allowed",
                OccurredAtUtc: DateTimeOffset.UtcNow),
            cancellationToken);

        return PrivilegedTenantOperationGuardResult.Allow();
    }

    private async Task<PrivilegedTenantOperationGuardResult> RejectAsync(
        Guid? userId,
        Guid? sourceTenantId,
        Guid targetTenantId,
        string action,
        int statusCode,
        string errorCode,
        string message,
        CancellationToken cancellationToken)
    {
        await auditSink.WriteAsync(
            new AuthorizationDecisionAuditEvent(
                UserId: userId,
                SourceTenantId: sourceTenantId,
                TargetTenantId: targetTenantId,
                Action: action,
                Outcome: "rejected",
                Reason: errorCode,
                OccurredAtUtc: DateTimeOffset.UtcNow),
            cancellationToken);

        return PrivilegedTenantOperationGuardResult.Reject(statusCode, errorCode, message);
    }
}
