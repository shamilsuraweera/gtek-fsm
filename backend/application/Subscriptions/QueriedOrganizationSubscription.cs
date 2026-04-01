namespace GTEK.FSM.Backend.Application.Subscriptions;

public sealed record QueriedOrganizationSubscription(
    Guid SubscriptionId,
    Guid TenantId,
    string PlanCode,
    int UserLimit,
    int ActiveUsers,
    int AvailableUserSlots,
    DateTime StartsOnUtc,
    DateTime? EndsOnUtc,
    string? RowVersion);