using GTEK.FSM.Backend.Domain.Enums;

namespace GTEK.FSM.Backend.Domain.Events;

public sealed record JobAssignmentStatusChangedDomainEvent(
    Guid JobId,
    Guid TenantId,
    AssignmentStatus PreviousStatus,
    AssignmentStatus CurrentStatus,
    Guid? WorkerUserId)
    : DomainEvent("JobAssignmentStatusChanged");
