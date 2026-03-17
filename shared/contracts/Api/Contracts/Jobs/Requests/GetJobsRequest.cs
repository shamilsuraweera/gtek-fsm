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
    /// <summary>
    /// Zero-based offset for pagination. Default: 0.
    /// </summary>
    public int Offset { get; set; } = 0;

    /// <summary>
    /// Maximum number of results to return (page size). Default: 20.
    /// </summary>
    public int Limit { get; set; } = 20;

    /// <summary>
    /// Optional filter by job status (e.g., "Pending", "InProgress", "Completed").
    /// If null, jobs of all statuses are returned.
    /// </summary>
    public string? StatusFilter { get; set; }

    /// <summary>
    /// Optional filter by worker ID.
    /// If null, jobs assigned to the current user/session context are returned.
    /// </summary>
    public string? WorkerIdFilter { get; set; }

    /// <summary>
    /// Optional sort column. Default: "AssignedUtc".
    /// </summary>
    public string? SortBy { get; set; } = "AssignedUtc";

    /// <summary>
    /// Sort direction: "asc" or "desc". Default: "asc".
    /// </summary>
    public string? SortDirection { get; set; } = "asc";
}
