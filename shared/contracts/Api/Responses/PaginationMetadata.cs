namespace GTEK.FSM.Shared.Contracts.Api.Responses;

/// <summary>
/// Standardized pagination metadata included in list responses.
/// 
/// Enables consistent offset-based pagination across all list endpoints.
/// Clients (web, mobile) use this metadata to:
/// - Display current page position and total count
/// - Build next/previous pagination controls
/// - Implement lazy loading or infinite scroll patterns
/// </summary>
public class PaginationMetadata
{
    /// <summary>
    /// Zero-based offset of the first item in the result set.
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// Maximum number of items returned in this result set (page size).
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// Total number of items matching the query filter (across all pages).
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Indicates whether more items exist beyond this page.
    /// </summary>
    public bool HasMore => Offset + Limit < Total;

    /// <summary>
    /// Calculated total number of pages based on Limit and Total.
    /// </summary>
    public int TotalPages => (Total + Limit - 1) / Limit;

    /// <summary>
    /// Calculated current page number (1-based) for display purposes.
    /// </summary>
    public int CurrentPage => (Offset / Limit) + 1;
}
