using GTEK.FSM.Backend.Domain.Events;
using GTEK.FSM.Backend.Domain.Rules;

namespace GTEK.FSM.Backend.Domain.Aggregates;

/// <summary>
/// Tenant aggregate root.
/// Owns tenant-level identity and links to the currently active subscription.
/// </summary>
public sealed class Tenant
{
    private readonly List<IDomainEvent> domainEvents = new();

    public Tenant(Guid id, string code, string name, Guid? activeSubscriptionId = null)
    {
        this.Id = DomainGuards.RequiredId(id, nameof(id), "Tenant id cannot be empty.");
        this.Code = DomainGuards.RequiredText(code, nameof(code), "Tenant code is required.", 32);
        this.Name = DomainGuards.RequiredText(name, nameof(name), "Tenant name is required.", 120);
        this.ActiveSubscriptionId = activeSubscriptionId;
    }

    public Guid Id { get; }

    public string Code { get; }

    public string Name { get; private set; }

    public Guid? ActiveSubscriptionId { get; private set; }

    public DateTime CreatedAtUtc { get; internal set; }

    public DateTime UpdatedAtUtc { get; internal set; }

    public bool IsDeleted { get; internal set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => this.domainEvents;

    public void Rename(string newName)
    {
        this.Name = DomainGuards.RequiredText(newName, nameof(newName), "Tenant name is required.", 120);
    }

    public void AttachSubscription(Guid subscriptionId)
    {
        var previous = this.ActiveSubscriptionId;
        this.ActiveSubscriptionId = DomainGuards.RequiredId(subscriptionId, nameof(subscriptionId), "Subscription id cannot be empty.");
        this.AddDomainEvent(new TenantSubscriptionChangedDomainEvent(this.Id, previous, this.ActiveSubscriptionId));
    }

    public void DetachSubscription()
    {
        var previous = this.ActiveSubscriptionId;
        this.ActiveSubscriptionId = null;
        this.AddDomainEvent(new TenantSubscriptionChangedDomainEvent(this.Id, previous, this.ActiveSubscriptionId));
    }

    public void ClearDomainEvents()
    {
        this.domainEvents.Clear();
    }

    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        this.domainEvents.Add(domainEvent);
    }
}
