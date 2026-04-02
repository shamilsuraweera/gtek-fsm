namespace GTEK.FSM.MobileApp.Navigation;

using GTEK.FSM.Shared.Contracts.Vocabulary;

public enum MobileSectionVisibility
{
    Both,
    CustomerOnly,
    WorkerOnly,
}

public static class RoleGateResolver
{
    public static MobileSectionVisibility Resolve(string rawRole)
    {
        var isWorker = ContainsRole(rawRole, "worker");
        var isCustomer = ContainsRole(rawRole, "customer");

        if (isWorker && !isCustomer)
        {
            return MobileSectionVisibility.WorkerOnly;
        }

        if (isCustomer && !isWorker)
        {
            return MobileSectionVisibility.CustomerOnly;
        }

        return MobileSectionVisibility.Both;
    }

    public static bool ContainsRole(string rawRole, string expectedRole)
    {
        if (string.IsNullOrWhiteSpace(rawRole))
        {
            return false;
        }

        var tokens = rawRole.Split(
            new[] { ',', ';', ' ', '|' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return tokens.Any(token => string.Equals(token, expectedRole, StringComparison.OrdinalIgnoreCase));
    }

    public static AuthorizationUxAccessState EvaluateActionAccess(string rawRole, bool tenantAllowed, params string[] requiredRoles)
    {
        var roleAllowed = requiredRoles.Any(requiredRole => ContainsRole(rawRole, requiredRole));
        return AuthorizationUxPolicy.EvaluateActionAccess(roleAllowed, tenantAllowed);
    }

    public static string BuildForbiddenFeedback(string rawRole, bool tenantAllowed, params string[] requiredRoles)
    {
        var accessState = EvaluateActionAccess(rawRole, tenantAllowed, requiredRoles);
        return AuthorizationUxPolicy.BuildForbiddenFeedback(accessState, tenantAllowed);
    }
}