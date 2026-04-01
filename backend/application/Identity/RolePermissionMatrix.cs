using GTEK.FSM.Shared.Contracts.Vocabulary;

namespace GTEK.FSM.Backend.Application.Identity;

public static class RolePermissionMatrix
{
    private static readonly IReadOnlyDictionary<UserRole, IReadOnlySet<string>> Matrix =
        new Dictionary<UserRole, IReadOnlySet<string>>
        {
            [UserRole.Guest] = Set(
                Permissions.SystemPing),

            [UserRole.Customer] = Set(
                Permissions.SystemPing,
                Permissions.RealTimeConnect,
                Permissions.ServiceRequestsRead,
                Permissions.ServiceRequestsWrite,
                Permissions.JobsRead),

            [UserRole.Worker] = Set(
                Permissions.SystemPing,
                Permissions.RealTimeConnect,
                Permissions.ServiceRequestsRead,
                Permissions.JobsRead,
                Permissions.JobsWrite),

            [UserRole.Support] = Set(
                Permissions.SystemPing,
                Permissions.RealTimeConnect,
                Permissions.TenantsRead,
                Permissions.UsersRead,
                Permissions.ServiceRequestsRead,
                Permissions.ServiceRequestsWrite,
                Permissions.JobsRead,
                Permissions.JobsWrite,
                Permissions.SubscriptionsRead),

            [UserRole.Manager] = Set(
                Permissions.SystemPing,
                Permissions.RealTimeConnect,
                Permissions.TenantsRead,
                Permissions.UsersRead,
                Permissions.UsersWrite,
                Permissions.ServiceRequestsRead,
                Permissions.ServiceRequestsWrite,
                Permissions.JobsRead,
                Permissions.JobsWrite,
                Permissions.SubscriptionsRead,
                Permissions.SubscriptionsWrite),

            [UserRole.Admin] = Set(
                Permissions.SystemPing,
                Permissions.RealTimeConnect,
                Permissions.TenantsRead,
                Permissions.TenantsWrite,
                Permissions.UsersRead,
                Permissions.UsersWrite,
                Permissions.ServiceRequestsRead,
                Permissions.ServiceRequestsWrite,
                Permissions.JobsRead,
                Permissions.JobsWrite,
                Permissions.SubscriptionsRead,
                Permissions.SubscriptionsWrite),
        };

    public static IReadOnlySet<string> GetPermissions(UserRole role)
    {
        return Matrix.TryGetValue(role, out var permissions)
            ? permissions
            : Set();
    }

    public static bool HasPermission(UserRole role, string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            return false;
        }

        return GetPermissions(role).Contains(permission.Trim());
    }

    private static IReadOnlySet<string> Set(params string[] permissions)
    {
        return new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase);
    }
}
