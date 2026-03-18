namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Users.Responses;

using GTEK.FSM.Shared.Contracts.Api.Responses;

/// <summary>
/// Response DTO for a single user in a list view.
/// 
/// Pattern: Returned as items within a paged list response
/// 
/// Demonstrates how user data is presented in list contexts,
/// exposing only necessary fields for display and action purposes.
/// </summary>
public class GetUsersResponse
{
    /// <summary>
    /// Unique identifier for the user.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Display name of the user.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Email address of the user.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Role assigned to this user (references UserRole vocabulary).
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// ID of the tenant this user belongs to.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Indicates whether this user account is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// UTC timestamp when the user account was created.
    /// </summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// Pagination metadata for the list context.
    /// </summary>
    public PaginationMetadata? Pagination { get; set; }
}
