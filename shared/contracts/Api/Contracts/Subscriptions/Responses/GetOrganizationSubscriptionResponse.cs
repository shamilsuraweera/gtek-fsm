namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Subscriptions.Responses;

public sealed class GetOrganizationSubscriptionResponse
{
    public string SubscriptionId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;

    public string PlanCode { get; set; } = string.Empty;

    public int UserLimit { get; set; }

    public int ActiveUsers { get; set; }

    public int AvailableUserSlots { get; set; }

    public DateTime StartsOnUtc { get; set; }

    public DateTime? EndsOnUtc { get; set; }

    public string? RowVersion { get; set; }
}