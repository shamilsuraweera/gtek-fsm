using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Responses;

namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Jobs.Responses;

/// <summary>
/// Response DTO for tenant-scoped job detail views.
/// </summary>
public class GetJobDetailResponse
{
    public string? JobId { get; set; }

    public string? TenantId { get; set; }

    public string? RequestId { get; set; }

    public string? AssignmentStatus { get; set; }

    public string? AssignedWorkerUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public string? RequestTitle { get; set; }

    public string? RequestStatus { get; set; }

    public IReadOnlyList<DetailTimelineItemResponse> Timeline { get; set; } = Array.Empty<DetailTimelineItemResponse>();
}