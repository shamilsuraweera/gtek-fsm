namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Responses;

/// <summary>
/// Response DTO for tenant-scoped service request detail views.
/// </summary>
public class GetServiceRequestDetailResponse
{
    public string? RequestId { get; set; }

    public string? RowVersion { get; set; }

    public string? TenantId { get; set; }

    public string? CustomerUserId { get; set; }

    public string? Title { get; set; }

    public string? Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public string? ActiveJobId { get; set; }

    public string? AssignedWorkerUserId { get; set; }

    public string? ActiveJobStatus { get; set; }

    public IReadOnlyList<DetailTimelineItemResponse> Timeline { get; set; } = Array.Empty<DetailTimelineItemResponse>();
}