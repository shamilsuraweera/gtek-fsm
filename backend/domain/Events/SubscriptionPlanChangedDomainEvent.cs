namespace GTEK.FSM.Backend.Domain.Events;

public sealed record SubscriptionPlanChangedDomainEvent(
    Guid SubscriptionId,
    Guid TenantId,
    string PreviousPlanCode,
    string CurrentPlanCode)
    : DomainEvent("SubscriptionPlanChanged");
