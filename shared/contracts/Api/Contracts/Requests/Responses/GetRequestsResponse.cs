namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Responses;

using GTEK.FSM.Shared.Contracts.Api.Responses;

/// <summary>
/// Response DTO for a single service request in a list context.
/// 
/// Pattern: Returned as items within a paged list response
/// 
/// This demonstrates the standard response DTO pattern:
/// - Naming: [Noun]Response (e.g., GetRequestsResponse, UserResponse)
/// - Contains only data needed for the specific endpoint (not full entity)
/// - Includes summarized data for list views, full data for detail views
/// - All timestamps use UTC DateTime
/// 
/// Usage in backend controller:
/// public async Task<ApiResponse<PaginatedList<GetRequestsResponse>>> GetRequests(...)
/// {
///     var items = /* fetch and map requests to GetRequestsResponse */
///     return new ApiResponse<PaginatedList<GetRequestsResponse>>
///     {
///         IsSuccess = true,
///         Data = PaginatedList.Create(items, pagination)
///     };
/// }
/// </summary>
public class GetRequestsResponse
{
    /// <summary>
    /// Unique identifier for the service request.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Current stage of the request (references RequestStage vocabulary).
    /// </summary>
    public string? Stage { get; set; }

    /// <summary>
    /// Brief summary or title of the request for list display.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// ID of the tenant owning this request.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// User role that created this request (references UserRole vocabulary).
    /// </summary>
    public string? CreatedByRole { get; set; }

    /// <summary>
    /// UTC timestamp when the request was created.
    /// </summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// UTC timestamp of the most recent update.
    /// </summary>
    public DateTime UpdatedUtc { get; set; }

    /// <summary>
    /// Metadata for the list page this item came from (set by controller).
    /// </summary>
    public PaginationMetadata? Pagination { get; set; }
}
