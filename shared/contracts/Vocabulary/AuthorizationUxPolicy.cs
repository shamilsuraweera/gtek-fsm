namespace GTEK.FSM.Shared.Contracts.Vocabulary;

/// <summary>
/// Canonical UI access states used across channels for role/tenant-gated actions.
/// </summary>
public enum AuthorizationUxAccessState
{
    Hidden = 0,
    Disabled = 1,
    Allowed = 2,
}


/// <summary>
/// Shared authorization UX messages and helpers to keep feedback consistent across clients.
/// </summary>
public static class AuthorizationUxPolicy
{
    public const string ForbiddenByRoleMessage = "Forbidden: your role does not have permission for this action.";

    public const string ForbiddenByTenantMessage = "Forbidden: your tenant scope does not allow this action.";

    public const string ForbiddenByRoleOrTenantMessage = "Forbidden: your role or tenant scope cannot modify this request.";

    public const string GuardrailAdminTenantMessage = "Forbidden: only admin scope in active tenant context can change governance guardrails.";

    public static AuthorizationUxAccessState EvaluateActionAccess(bool roleAllowed, bool tenantAllowed)
    {
        if (!roleAllowed)
        {
            return AuthorizationUxAccessState.Hidden;
        }

        if (!tenantAllowed)
        {
            return AuthorizationUxAccessState.Disabled;
        }

        return AuthorizationUxAccessState.Allowed;
    }

    public static string BuildForbiddenFeedback(AuthorizationUxAccessState accessState, bool tenantAllowed)
    {
        if (accessState == AuthorizationUxAccessState.Allowed)
        {
            return string.Empty;
        }

        return tenantAllowed ? ForbiddenByRoleMessage : ForbiddenByTenantMessage;
    }
}