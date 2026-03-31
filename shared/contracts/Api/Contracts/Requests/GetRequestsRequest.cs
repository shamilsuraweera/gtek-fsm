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
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 25;

    /// <summary>
    /// Optional filter by request stage (maps to RequestStage enum values).
    /// If null, requests of all stages are returned.
    /// </summary>
    public string? StatusFilter { get; set; }

    public DateTime? CreatedFromUtc { get; set; }

    public DateTime? CreatedToUtc { get; set; }

    public string? AssignedWorkerUserIdFilter { get; set; }

    public string? SearchText { get; set; }

    public string? SortBy { get; set; } = "createdAtUtc";

    public string? SortDirection { get; set; } = "desc";
}
