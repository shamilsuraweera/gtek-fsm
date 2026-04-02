namespace GTEK.FSM.Backend.Application.ServiceRequests;

public sealed record SlaEscalationTriggeredPayload(
    Guid RequestId,
    Guid TenantId,
    string SlaDimension,
    string PreviousSlaStatus,
    string CurrentSlaStatus,
    DateTime? DueAtUtc,
    DateTime TriggeredAtUtc,
    string? RowVersion);
