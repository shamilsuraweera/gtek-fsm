using GTEK.FSM.Backend.Domain.Rules;

namespace GTEK.FSM.Backend.Domain.Aggregates;

/// <summary>
/// User aggregate root.
/// Always belongs to exactly one tenant.
/// </summary>
public sealed class User
{
    public User(Guid id, Guid tenantId, string externalIdentity, string displayName)
    {
        this.Id = DomainGuards.RequiredId(id, nameof(id), "User id cannot be empty.");
        this.TenantId = DomainGuards.RequiredId(tenantId, nameof(tenantId), "User must belong to a tenant.");
        this.ExternalIdentity = DomainGuards.RequiredText(externalIdentity, nameof(externalIdentity), "External identity is required.", 128);
        this.DisplayName = DomainGuards.RequiredText(displayName, nameof(displayName), "Display name is required.", 120);
    }

    public Guid Id { get; }

    public Guid TenantId { get; }

    public string ExternalIdentity { get; }

    public string DisplayName { get; private set; }

    public DateTime CreatedAtUtc { get; internal set; }

    public DateTime UpdatedAtUtc { get; internal set; }

    public bool IsDeleted { get; internal set; }

    public void Rename(string displayName)
    {
        this.DisplayName = DomainGuards.RequiredText(displayName, nameof(displayName), "Display name is required.", 120);
    }
}
