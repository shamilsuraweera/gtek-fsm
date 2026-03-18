namespace GTEK.FSM.Backend.Domain.Aggregates;

/// <summary>
/// Subscription aggregate root.
/// Belongs to one tenant and defines commercial plan boundaries.
/// </summary>
public sealed class Subscription
{
    public Subscription(Guid id, Guid tenantId, string planCode, DateTime startsOnUtc, DateTime? endsOnUtc = null)
    {
        this.Id = id != Guid.Empty ? id : throw new ArgumentException("Subscription id cannot be empty.", nameof(id));
        this.TenantId = tenantId != Guid.Empty ? tenantId : throw new ArgumentException("Subscription must belong to a tenant.", nameof(tenantId));
        this.PlanCode = !string.IsNullOrWhiteSpace(planCode) ? planCode.Trim() : throw new ArgumentException("Plan code is required.", nameof(planCode));
        this.StartsOnUtc = startsOnUtc;
        this.EndsOnUtc = endsOnUtc;

        if (this.EndsOnUtc.HasValue && this.EndsOnUtc.Value < this.StartsOnUtc)
        {
            throw new ArgumentException("Subscription end date cannot be before start date.", nameof(endsOnUtc));
        }
    }

    public Guid Id { get; }

    public Guid TenantId { get; }

    public string PlanCode { get; private set; }

    public DateTime StartsOnUtc { get; }

    public DateTime? EndsOnUtc { get; private set; }

    public void ChangePlan(string planCode)
    {
        this.PlanCode = !string.IsNullOrWhiteSpace(planCode) ? planCode.Trim() : throw new ArgumentException("Plan code is required.", nameof(planCode));
    }

    public void End(DateTime endsOnUtc)
    {
        if (endsOnUtc < this.StartsOnUtc)
        {
            throw new ArgumentException("Subscription end date cannot be before start date.", nameof(endsOnUtc));
        }

        this.EndsOnUtc = endsOnUtc;
    }
}
