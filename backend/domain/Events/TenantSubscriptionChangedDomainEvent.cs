namespace GTEK.FSM.Backend.Domain.Events;

public sealed record TenantSubscriptionChangedDomainEvent(
    Guid TenantId,
    Guid? PreviousSubscriptionId,
    Guid? CurrentSubscriptionId)
    : DomainEvent("TenantSubscriptionChanged");
