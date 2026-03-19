namespace GTEK.FSM.Backend.Application.Identity;

public sealed record PrivilegedTenantOperationRequest(Guid TargetTenantId, string Action);

public sealed record PrivilegedTenantOperationGuardResult(
    bool IsAllowed,
    int? StatusCode,
    string? ErrorCode,
    string? Message)
{
    public static PrivilegedTenantOperationGuardResult Allow()
    {
        return new PrivilegedTenantOperationGuardResult(true, null, null, null);
    }

    public static PrivilegedTenantOperationGuardResult Reject(int statusCode, string errorCode, string message)
    {
        return new PrivilegedTenantOperationGuardResult(false, statusCode, errorCode, message);
    }
}
