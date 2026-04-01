namespace GTEK.FSM.Backend.Application.Audit;

public sealed record QueriedAuditLogItem(
    Guid AuditLogId,
    Guid TenantId,
    Guid? ActorUserId,
    string EntityType,
    Guid EntityId,
    string Action,
    string Outcome,
    DateTimeOffset OccurredAtUtc,
    string? Details);
