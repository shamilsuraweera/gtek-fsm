namespace GTEK.FSM.Shared.Contracts.Api.Responses;

/// <summary>
/// Generic base response envelope for all API responses.
/// 
/// Ensures consistency across all endpoints by providing:
/// - IsSuccess flag for quick success/failure detection
/// - Message for user-friendly feedback or error descriptions
/// - Data generic property for typed payload
/// - Timestamp for response tracking and debugging
/// 
/// Backend middleware will serialize all responses through this envelope
/// so clients (web, mobile) receive a consistent response shape.
/// </summary>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the request was processed successfully.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// User-facing or debug message describing the response state or any error.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Typed response data payload. Null if IsSuccess is false or no data applies.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// UTC timestamp when the response was generated (set by middleware).
    /// </summary>
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    // Future extensibility:
    // - public string? ErrorCode { get; set; } — for fine-grained error categorization
    // - public Dictionary<string, object>? Metadata { get; set; } — for cross-cutting properties
}

/// <summary>
/// Non-generic variant for responses with no data payload.
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// Indicates whether the request was processed successfully.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// User-facing or debug message describing the response state or any error.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// UTC timestamp when the response was generated (set by middleware).
    /// </summary>
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
