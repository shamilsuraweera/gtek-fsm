namespace GTEK.FSM.Backend.Application.Persistence.Specifications;

public sealed record AuditLogQuerySpecification(
    Guid TenantId,
    Guid? ActorUserId = null,
    string? EntityType = null,
    Guid? EntityId = null,
    string? Action = null,
    string? Outcome = null,
    DateTimeOffset? OccurredFromUtc = null,
    DateTimeOffset? OccurredToUtc = null,
    PageSpecification? Page = null);
