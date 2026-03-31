namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Jobs.Requests;

/// <summary>
/// Request contract for listing jobs assigned to a worker with optional filtering.
/// 
/// Pattern: GET /api/v1/jobs
/// 
/// Follows the same structure as GetRequestsRequest, demonstrating
/// cross-feature consistency in pagination, filtering, and sorting.
/// </summary>
public class GetJobsRequest
{
    public int? Page { get; set; }

    public int? PageSize { get; set; }

    /// <summary>
    /// Optional filter by job status (e.g., "Pending", "InProgress", "Completed").
    /// If null, jobs of all statuses are returned.
    /// </summary>
    public string? StatusFilter { get; set; }

    public string? WorkerIdFilter { get; set; }

    public DateTime? ScheduledFromUtc { get; set; }

    public DateTime? ScheduledToUtc { get; set; }

    public string? SearchText { get; set; }

    public string? SortBy { get; set; } = "createdAtUtc";

    public string? SortDirection { get; set; } = "desc";
}
