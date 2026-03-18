namespace GTEK.FSM.Backend.Domain.Aggregates;

/// <summary>
/// User aggregate root.
/// Always belongs to exactly one tenant.
/// </summary>
public sealed class User
{
    public User(Guid id, Guid tenantId, string externalIdentity, string displayName)
    {
        this.Id = id != Guid.Empty ? id : throw new ArgumentException("User id cannot be empty.", nameof(id));
        this.TenantId = tenantId != Guid.Empty ? tenantId : throw new ArgumentException("User must belong to a tenant.", nameof(tenantId));
        this.ExternalIdentity = !string.IsNullOrWhiteSpace(externalIdentity)
            ? externalIdentity.Trim()
            : throw new ArgumentException("External identity is required.", nameof(externalIdentity));
        this.DisplayName = !string.IsNullOrWhiteSpace(displayName)
            ? displayName.Trim()
            : throw new ArgumentException("Display name is required.", nameof(displayName));
    }

    public Guid Id { get; }

    public Guid TenantId { get; }

    public string ExternalIdentity { get; }

    public string DisplayName { get; private set; }

    public void Rename(string displayName)
    {
        this.DisplayName = !string.IsNullOrWhiteSpace(displayName)
            ? displayName.Trim()
            : throw new ArgumentException("Display name is required.", nameof(displayName));
    }
}
