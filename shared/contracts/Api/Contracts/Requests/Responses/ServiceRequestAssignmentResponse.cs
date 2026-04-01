namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Responses;

/// <summary>
/// Response payload returned after assignment or reassignment operations.
/// </summary>
public sealed class ServiceRequestAssignmentResponse
{
    public string RequestId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;

    public string JobId { get; set; } = string.Empty;

    public string? PreviousWorkerUserId { get; set; }

    public string CurrentWorkerUserId { get; set; } = string.Empty;

    public string AssignmentStatus { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; }

    public string? RowVersion { get; set; }
}
