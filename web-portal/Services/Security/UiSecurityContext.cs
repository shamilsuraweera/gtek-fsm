//-----------------------------------------------------------------------
// <copyright file="UiSecurityContext.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using GTEK.FSM.Shared.Contracts.Vocabulary;

namespace GTEK.FSM.WebPortal.Services.Security;

/// <summary>
/// Provides a lightweight UI security context for role and tenant-aware action gating.
/// </summary>
public sealed class UiSecurityContext
{
    private static readonly HashSet<string> ElevatedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        PortalRole.Manager,
        PortalRole.Admin,
    };

    private readonly PortalAuthState authState;

    public UiSecurityContext(PortalAuthState authState)
    {
        this.authState = authState;
    }

    /// <summary>
    /// Gets the current role for this UI context.
    /// </summary>
    public string CurrentRole => this.authState.CurrentRole;

    /// <summary>
    /// Gets the active tenant context for this operator session.
    /// </summary>
    public string CurrentTenantId => this.authState.CurrentTenantId;

    /// <summary>
    /// Gets the tenant IDs this operator can access.
    /// </summary>
    public IReadOnlySet<string> AccessibleTenants => string.IsNullOrWhiteSpace(this.CurrentTenantId)
        ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        : new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            this.CurrentTenantId,
        };

    /// <summary>
    /// Determines whether the active role has elevated access for sensitive operations.
    /// </summary>
    /// <returns><see langword="true"/> when elevated role requirements are met; otherwise <see langword="false"/>.</returns>
    public bool HasSensitiveAccess()
    {
        return ElevatedRoles.Contains(this.CurrentRole);
    }

    /// <summary>
    /// Evaluates channel-consistent action access by role and tenant scope.
    /// </summary>
    /// <param name="tenantId">The tenant context tied to the action.</param>
    /// <param name="allowedRoles">Roles that can perform the action.</param>
    /// <returns>The canonical UX access state.</returns>
    public AuthorizationUxAccessState GetActionAccessState(string tenantId, params string[] allowedRoles)
    {
        var roleAllowed = this.HasAnyRole(allowedRoles);
        var tenantAllowed = this.CanAccessTenant(tenantId);
        return AuthorizationUxPolicy.EvaluateActionAccess(roleAllowed, tenantAllowed);
    }

    /// <summary>
    /// Returns canonical forbidden feedback for denied access decisions.
    /// </summary>
    /// <param name="tenantId">The tenant context tied to the action.</param>
    /// <param name="allowedRoles">Roles that can perform the action.</param>
    /// <returns>Shared feedback copy when access is denied; otherwise empty string.</returns>
    public string GetForbiddenFeedback(string tenantId, params string[] allowedRoles)
    {
        var tenantAllowed = this.CanAccessTenant(tenantId);
        var accessState = this.GetActionAccessState(tenantId, allowedRoles);
        return AuthorizationUxPolicy.BuildForbiddenFeedback(accessState, tenantAllowed);
    }

    /// <summary>
    /// Determines whether the current role matches any allowed role.
    /// </summary>
    /// <param name="allowedRoles">The permitted roles for an action.</param>
    /// <returns><see langword="true"/> if the current role is allowed; otherwise <see langword="false"/>.</returns>
    public bool HasAnyRole(params string[] allowedRoles)
    {
        if (allowedRoles.Length == 0)
        {
            return false;
        }

        return allowedRoles.Any(role => string.Equals(role, this.CurrentRole, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines whether the active tenant context can access the requested tenant.
    /// </summary>
    /// <param name="tenantId">The tenant to evaluate.</param>
    /// <returns><see langword="true"/> when access is permitted; otherwise <see langword="false"/>.</returns>
    public bool CanAccessTenant(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return false;
        }

        return string.Equals(this.CurrentRole, PortalRole.Admin, StringComparison.OrdinalIgnoreCase)
            || this.AccessibleTenants.Contains(tenantId);
    }
}

