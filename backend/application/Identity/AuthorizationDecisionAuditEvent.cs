namespace GTEK.FSM.Backend.Application.Identity;

public sealed record AuthorizationDecisionAuditEvent(
    Guid? UserId,
    Guid? SourceTenantId,
    Guid? TargetTenantId,
    string Action,
    string Outcome,
    string Reason,
    DateTimeOffset OccurredAtUtc);
