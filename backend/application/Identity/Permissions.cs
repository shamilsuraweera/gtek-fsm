namespace GTEK.FSM.Backend.Application.Identity;

/// <summary>
/// Baseline permission catalog for Phase 2 authorization.
/// </summary>
public static class Permissions
{
    public const string SystemPing = "system.ping";

    public const string TenantsRead = "tenants.read";
    public const string TenantsWrite = "tenants.write";

    public const string UsersRead = "users.read";
    public const string UsersWrite = "users.write";

    public const string ServiceRequestsRead = "service_requests.read";
    public const string ServiceRequestsWrite = "service_requests.write";

    public const string JobsRead = "jobs.read";
    public const string JobsWrite = "jobs.write";

    public const string SubscriptionsRead = "subscriptions.read";
    public const string SubscriptionsWrite = "subscriptions.write";
}
