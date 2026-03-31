namespace GTEK.FSM.Backend.Application.Subscriptions;

public sealed record QueriedSubscriptionUserItem(
    Guid UserId,
    string DisplayName,
    string ExternalIdentity,
    bool IsWithinCurrentPlanLimit);