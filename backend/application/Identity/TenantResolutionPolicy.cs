namespace GTEK.FSM.Backend.Application.Identity;

public static class TenantResolutionPolicy
{
    public static TenantResolutionOutcome Resolve(TenantResolutionInput input)
    {
        if (!string.IsNullOrWhiteSpace(input.TenantClaimValue))
        {
            if (!Guid.TryParse(input.TenantClaimValue.Trim(), out var claimTenantId))
            {
                return TenantResolutionOutcome.Failure(
                    statusCode: 401,
                    errorCode: "MALFORMED_TENANT_CLAIM",
                    message: "Claim 'tenant_id' must be a valid GUID.");
            }

            return TenantResolutionOutcome.Success(claimTenantId);
        }

        if (string.IsNullOrWhiteSpace(input.TenantHeaderValue))
        {
            return TenantResolutionOutcome.Failure(
                statusCode: 401,
                errorCode: "TENANT_CONTEXT_UNRESOLVED",
                message: "Tenant context could not be resolved from authenticated claims.");
        }

        if (!input.AllowHeaderFallback)
        {
            return TenantResolutionOutcome.Failure(
                statusCode: 403,
                errorCode: "TENANT_HEADER_FALLBACK_NOT_ALLOWED",
                message: "Header-based tenant override is not allowed for current principal.");
        }

        if (!Guid.TryParse(input.TenantHeaderValue.Trim(), out var headerTenantId))
        {
            return TenantResolutionOutcome.Failure(
                statusCode: 400,
                errorCode: "MALFORMED_TENANT_HEADER",
                message: "Header tenant id must be a valid GUID.");
        }

        return TenantResolutionOutcome.Success(headerTenantId);
    }
}

public sealed record TenantResolutionInput(
    string? TenantClaimValue,
    string? TenantHeaderValue,
    bool AllowHeaderFallback);

public sealed record TenantResolutionOutcome(
    bool IsSuccess,
    Guid? TenantId,
    int? StatusCode,
    string? ErrorCode,
    string? Message)
{
    public static TenantResolutionOutcome Success(Guid tenantId)
    {
        return new TenantResolutionOutcome(true, tenantId, null, null, null);
    }

    public static TenantResolutionOutcome Failure(int statusCode, string errorCode, string message)
    {
        return new TenantResolutionOutcome(false, null, statusCode, errorCode, message);
    }
}
