namespace GTEK.FSM.Shared.Contracts.Api.Requests;

/// <summary>
/// Base class for standardized API request envelopes.
/// 
/// All API requests should inherit from or follow this pattern to ensure:
/// - Consistent naming and structure across endpoints
/// - Future extensibility for cross-cutting concerns (tracing, auditing, pagination info)
/// - Type-safe consumption in backend controllers and services
/// 
/// Current usage: requests inherit directly from their specific request classes
/// Future usage: add properties for request ID tracking, correlation IDs, or optional metadata
/// </summary>
public abstract class ApiRequest
{
    // Placeholder: This base class is extensible for future cross-cutting request properties.
    // Example future properties:
    // - public string? CorrelationId { get; set; }
    // - public string? TraceId { get; set; }
}
