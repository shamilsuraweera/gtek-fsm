namespace GTEK.FSM.Backend.Application.Identity;

public sealed record TenantOwnershipGuardResult(
    bool IsAllowed,
    int? StatusCode,
    string? ErrorCode,
    string? Message)
{
    public static TenantOwnershipGuardResult Allow()
    {
        return new TenantOwnershipGuardResult(true, null, null, null);
    }

    public static TenantOwnershipGuardResult Reject(int statusCode, string errorCode, string message)
    {
        return new TenantOwnershipGuardResult(false, statusCode, errorCode, message);
    }
}
