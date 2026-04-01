namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Audit.Requests;

public sealed class GetAuditLogsRequest
{
    public string? ActorUserId { get; set; }

    public string? EntityType { get; set; }

    public string? EntityId { get; set; }

    public string? Action { get; set; }

    public string? Outcome { get; set; }

    public DateTimeOffset? FromUtc { get; set; }

    public DateTimeOffset? ToUtc { get; set; }

    public int? Page { get; set; }

    public int? PageSize { get; set; }
}
