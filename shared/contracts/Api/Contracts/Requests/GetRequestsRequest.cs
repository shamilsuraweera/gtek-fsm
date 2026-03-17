namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Requests;

/// <summary>
/// Request contract for listing service requests with optional filtering and pagination.
/// 
/// Pattern: GET /api/v1/requests
/// 
/// This demonstrates the standard request DTO pattern:
/// - Query parameters are mapped as properties of the request class
/// - Naming: [Verb][Noun]Request (e.g., GetRequestsRequest, CreateRequestRequest)
/// - Pagination parameters (offset, limit) are included for list endpoints
/// - Optional properties use 'bool?' and 'string?' for filter criteria
/// 
/// Usage in backend controller:
/// [HttpGet]
/// public async Task<ApiResponse<PagedResult<GetRequestsResponse>>> GetRequests(
///     [FromQuery] GetRequestsRequest request)
/// {
///     // Map to query and execute, return ApiResponse<PagedResult<GetRequestsResponse>>
/// }
/// </summary>
public class GetRequestsRequest
{
    /// <summary>
    /// Zero-based offset for pagination. Default: 0.
    /// </summary>
    public int Offset { get; set; } = 0;

    /// <summary>
    /// Maximum number of results to return (page size). Default: 10. Max: 100.
    /// </summary>
    public int Limit { get; set; } = 10;

    /// <summary>
    /// Optional filter by request stage (maps to RequestStage enum values).
    /// If null, requests of all stages are returned.
    /// </summary>
    public string? StageFilter { get; set; }

    /// <summary>
    /// Optional filter by tenant ID.
    /// If null, requests from all accessible tenants are returned.
    /// </summary>
    public string? TenantIdFilter { get; set; }

    /// <summary>
    /// Optional sort order column name (e.g., "CreatedUtc", "UpdatedUtc", "Stage").
    /// Default: "CreatedUtc".
    /// </summary>
    public string? SortBy { get; set; } = "CreatedUtc";

    /// <summary>
    /// Sort direction: "asc" or "desc". Default: "desc".
    /// </summary>
    public string? SortDirection { get; set; } = "desc";
}
