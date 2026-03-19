namespace GTEK.FSM.Backend.Application.Identity;

public static class AuthorizationPolicyCatalog
{
    public const string SystemPing = "policy.system.ping";

    public const string CustomerFlow = "policy.customer.flow";
    public const string WorkerFlow = "policy.worker.flow";
    public const string SupportFlow = "policy.support.flow";
    public const string ManagementFlow = "policy.management.flow";
    public const string AdminFlow = "policy.admin.flow";

    private static readonly IReadOnlyDictionary<string, string> PolicyToPermissionMap =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [SystemPing] = Permissions.SystemPing,
            [CustomerFlow] = Permissions.ServiceRequestsWrite,
            [WorkerFlow] = Permissions.JobsWrite,
            [SupportFlow] = Permissions.ServiceRequestsWrite,
            [ManagementFlow] = Permissions.UsersWrite,
            [AdminFlow] = Permissions.TenantsWrite,
        };

    public static IReadOnlyDictionary<string, string> GetPolicyPermissions()
    {
        return PolicyToPermissionMap;
    }
}
