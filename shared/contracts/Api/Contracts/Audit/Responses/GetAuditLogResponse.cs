namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Audit.Responses;

public sealed class GetAuditLogResponse
{
    public string? AuditLogId { get; set; }

    public string? TenantId { get; set; }

    public string? ActorUserId { get; set; }

    public string? EntityType { get; set; }

    public string? EntityId { get; set; }

    public string? Action { get; set; }

    public string? Outcome { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    public string? Details { get; set; }
}
