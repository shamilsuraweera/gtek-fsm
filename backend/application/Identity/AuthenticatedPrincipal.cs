namespace GTEK.FSM.Backend.Application.Identity;

/// <summary>
/// Transport-agnostic authenticated principal model for application-layer authorization decisions.
/// </summary>
public sealed class AuthenticatedPrincipal
{
    private readonly HashSet<string> roles;
    private readonly HashSet<string> scopes;

    public AuthenticatedPrincipal(
        Guid userId,
        Guid tenantId,
        IEnumerable<string>? roles = null,
        IEnumerable<string>? scopes = null)
    {
        this.UserId = userId != Guid.Empty
            ? userId
            : throw new ArgumentException("User id cannot be empty.", nameof(userId));

        this.TenantId = tenantId != Guid.Empty
            ? tenantId
            : throw new ArgumentException("Tenant id cannot be empty.", nameof(tenantId));

        this.roles = NormalizeValues(roles);
        this.scopes = NormalizeValues(scopes);
    }

    public Guid UserId { get; }

    public Guid TenantId { get; }

    public IReadOnlySet<string> Roles => this.roles;

    public IReadOnlySet<string> Scopes => this.scopes;

    public bool IsInRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        return this.roles.Contains(role.Trim());
    }

    public bool HasScope(string scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            return false;
        }

        return this.scopes.Contains(scope.Trim());
    }

    private static HashSet<string> NormalizeValues(IEnumerable<string>? values)
    {
        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (values is null)
        {
            return normalized;
        }

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            normalized.Add(value.Trim());
        }

        return normalized;
    }
}
