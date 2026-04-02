using GTEK.FSM.Backend.Domain.Enums;

namespace GTEK.FSM.Backend.Application.ServiceRequests;

/// <summary>
/// Represents a single SLA state transition that qualifies as an escalation.
/// </summary>
internal sealed record SlaEscalationTransition(
    string SlaDimension,
    SlaState PreviousState,
    SlaState CurrentState,
    DateTime? DueAtUtc);
