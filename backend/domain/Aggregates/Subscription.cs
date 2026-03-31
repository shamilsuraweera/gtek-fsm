using GTEK.FSM.Backend.Domain.Events;
using GTEK.FSM.Backend.Domain.Rules;

namespace GTEK.FSM.Backend.Domain.Aggregates;

/// <summary>
/// Subscription aggregate root.
/// Belongs to one tenant and defines commercial plan boundaries.
/// </summary>
public sealed class Subscription
{
    private readonly List<IDomainEvent> domainEvents = new();
    private const int MinUserLimit = 1;
    private const int MaxUserLimit = 10000;

    public Subscription(Guid id, Guid tenantId, string planCode, DateTime startsOnUtc, DateTime? endsOnUtc = null, int userLimit = 25)
    {
        this.Id = DomainGuards.RequiredId(id, nameof(id), "Subscription id cannot be empty.");
        this.TenantId = DomainGuards.RequiredId(tenantId, nameof(tenantId), "Subscription must belong to a tenant.");
        this.PlanCode = DomainGuards.RequiredText(planCode, nameof(planCode), "Plan code is required.", 32);
        this.StartsOnUtc = startsOnUtc;
        this.EndsOnUtc = endsOnUtc;
        this.UserLimit = NormalizeUserLimit(userLimit);

        if (this.EndsOnUtc.HasValue && this.EndsOnUtc.Value < this.StartsOnUtc)
        {
            throw new ArgumentException("Subscription end date cannot be before start date.", nameof(endsOnUtc));
        }
    }

    public Guid Id { get; }

    public Guid TenantId { get; }

    public string PlanCode { get; private set; }

    public int UserLimit { get; private set; }

    public DateTime StartsOnUtc { get; }

    public DateTime? EndsOnUtc { get; private set; }

    public DateTime CreatedAtUtc { get; internal set; }

    public DateTime UpdatedAtUtc { get; internal set; }

    public bool IsDeleted { get; internal set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => this.domainEvents;

    public void ChangePlan(string planCode)
    {
        if (this.EndsOnUtc.HasValue)
        {
            throw new InvalidOperationException("Cannot change plan of an ended subscription.");
        }

        var previousPlanCode = this.PlanCode;
        this.PlanCode = DomainGuards.RequiredText(planCode, nameof(planCode), "Plan code is required.", 32);
        this.AddDomainEvent(new SubscriptionPlanChangedDomainEvent(this.Id, this.TenantId, previousPlanCode, this.PlanCode));
    }

    public void End(DateTime endsOnUtc)
    {
        if (this.EndsOnUtc.HasValue)
        {
            throw new InvalidOperationException("Subscription is already ended.");
        }

        if (endsOnUtc < this.StartsOnUtc)
        {
            throw new ArgumentException("Subscription end date cannot be before start date.", nameof(endsOnUtc));
        }

        this.EndsOnUtc = endsOnUtc;
    }

    public void ChangeUserLimit(int userLimit)
    {
        this.UserLimit = NormalizeUserLimit(userLimit);
    }

    public void ClearDomainEvents()
    {
        this.domainEvents.Clear();
    }

    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        this.domainEvents.Add(domainEvent);
    }

    private static int NormalizeUserLimit(int userLimit)
    {
        if (userLimit < MinUserLimit || userLimit > MaxUserLimit)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userLimit),
                $"User limit must be between {MinUserLimit} and {MaxUserLimit}.");
        }

        return userLimit;
    }
}
