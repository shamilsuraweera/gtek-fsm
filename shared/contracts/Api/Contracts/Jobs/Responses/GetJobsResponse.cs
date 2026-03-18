namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Jobs.Responses;

using GTEK.FSM.Shared.Contracts.Api.Responses;

/// <summary>
/// Response DTO for a single job in a worker's list view.
/// 
/// Pattern: Returned as items within a paged list response
/// 
/// Demonstrates consistent response DTO structure across features,
/// ensuring clients (web, mobile) expect similar shapes and pagination patterns.
/// </summary>
public class GetJobsResponse
{
    /// <summary>
    /// Unique identifier for the job.
    /// </summary>
    public string? JobId { get; set; }

    /// <summary>
    /// Brief description or title of the job for list display.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Current status of the job (e.g., "New", "InProgress", "Completed").
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Underlying request ID that generated this job (cross-reference).
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// ID of the assigned worker.
    /// </summary>
    public string? AssignedTo { get; set; }

    /// <summary>
    /// UTC timestamp when the job was created or assigned.
    /// </summary>
    public DateTime AssignedUtc { get; set; }

    /// <summary>
    /// Pagination metadata for the list context.
    /// </summary>
    public PaginationMetadata? Pagination { get; set; }
}
