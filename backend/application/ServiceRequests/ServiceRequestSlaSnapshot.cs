using GTEK.FSM.Backend.Domain.Enums;

namespace GTEK.FSM.Backend.Application.ServiceRequests;

public sealed record ServiceRequestSlaSnapshot(
    DateTime? ResponseDueAtUtc,
    DateTime? AssignmentDueAtUtc,
    DateTime? CompletionDueAtUtc,
    SlaState ResponseSlaState,
    SlaState AssignmentSlaState,
    SlaState CompletionSlaState,
    DateTime? NextSlaDeadlineAtUtc);
