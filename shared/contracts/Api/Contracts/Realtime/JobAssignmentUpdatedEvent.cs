namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Realtime;

public sealed class JobAssignmentUpdatedEvent
{
    public string RequestId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;

    public string JobId { get; set; } = string.Empty;

    public string? PreviousWorkerUserId { get; set; }

    public string? CurrentWorkerUserId { get; set; }

    public string AssignmentStatus { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; }

    public string? RowVersion { get; set; }
}