using GTEK.FSM.Shared.Contracts.Vocabulary;

namespace GTEK.FSM.Backend.Application.Identity;

public static class RolePermissionAuthorizer
{
    public static bool IsAuthorizedForPermission(IEnumerable<string> roles, string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            return false;
        }

        foreach (var role in roles)
        {
            if (!Enum.TryParse<UserRole>(role.Trim(), ignoreCase: true, out var parsedRole))
            {
                continue;
            }

            if (RolePermissionMatrix.HasPermission(parsedRole, permission))
            {
                return true;
            }
        }

        return false;
    }
}
