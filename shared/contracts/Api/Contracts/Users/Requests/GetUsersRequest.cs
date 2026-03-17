namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Users.Requests;

/// <summary>
/// Request contract for listing users with optional role and status filtering.
/// 
/// Pattern: GET /api/v1/users
/// 
/// Demonstrates role-based filtering and active/inactive status filtering,
/// showing how vocabulary enums are referenced in contract DTOs.
/// </summary>
public class GetUsersRequest
{
    /// <summary>
    /// Zero-based offset for pagination. Default: 0.
    /// </summary>
    public int Offset { get; set; } = 0;

    /// <summary>
    /// Maximum number of results to return (page size). Default: 25.
    /// </summary>
    public int Limit { get; set; } = 25;

    /// <summary>
    /// Optional filter by user role (references UserRole vocabulary).
    /// If null, users of all roles are returned.
    /// </summary>
    public string? RoleFilter { get; set; }

    /// <summary>
    /// Optional filter by tenant ID.
    /// If null, users from all accessible tenants are returned.
    /// </summary>
    public string? TenantIdFilter { get; set; }

    /// <summary>
    /// Optional filter by active status. Null = all users, true = active only, false = inactive.
    /// </summary>
    public bool? IsActiveFilter { get; set; }

    /// <summary>
    /// Optional search term for name or email matching.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Optional sort column. Default: "CreatedUtc".
    /// </summary>
    public string? SortBy { get; set; } = "CreatedUtc";

    /// <summary>
    /// Sort direction: "asc" or "desc". Default: "desc".
    /// </summary>
    public string? SortDirection { get; set; } = "desc";
}
