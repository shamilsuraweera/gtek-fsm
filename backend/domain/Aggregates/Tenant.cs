namespace GTEK.FSM.Backend.Domain.Aggregates;

/// <summary>
/// Tenant aggregate root.
/// Owns tenant-level identity and links to the currently active subscription.
/// </summary>
public sealed class Tenant
{
    public Tenant(Guid id, string code, string name, Guid? activeSubscriptionId = null)
    {
        this.Id = id != Guid.Empty ? id : throw new ArgumentException("Tenant id cannot be empty.", nameof(id));
        this.Code = !string.IsNullOrWhiteSpace(code) ? code.Trim() : throw new ArgumentException("Tenant code is required.", nameof(code));
        this.Name = !string.IsNullOrWhiteSpace(name) ? name.Trim() : throw new ArgumentException("Tenant name is required.", nameof(name));
        this.ActiveSubscriptionId = activeSubscriptionId;
    }

    public Guid Id { get; }

    public string Code { get; }

    public string Name { get; private set; }

    public Guid? ActiveSubscriptionId { get; private set; }

    public void Rename(string newName)
    {
        this.Name = !string.IsNullOrWhiteSpace(newName) ? newName.Trim() : throw new ArgumentException("Tenant name is required.", nameof(newName));
    }

    public void AttachSubscription(Guid subscriptionId)
    {
        this.ActiveSubscriptionId = subscriptionId != Guid.Empty
            ? subscriptionId
            : throw new ArgumentException("Subscription id cannot be empty.", nameof(subscriptionId));
    }

    public void DetachSubscription()
    {
        this.ActiveSubscriptionId = null;
    }
}
