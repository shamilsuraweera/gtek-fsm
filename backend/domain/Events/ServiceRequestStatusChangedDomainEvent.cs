using GTEK.FSM.Backend.Domain.Enums;

namespace GTEK.FSM.Backend.Domain.Events;

public sealed record ServiceRequestStatusChangedDomainEvent(
    Guid RequestId,
    Guid TenantId,
    ServiceRequestStatus PreviousStatus,
    ServiceRequestStatus CurrentStatus)
    : DomainEvent("ServiceRequestStatusChanged");
