namespace GTEK.FSM.Backend.Application.ServiceRequests;

/// <summary>
/// Represents a tenant-scoped job detail payload.
/// </summary>
public sealed record QueriedJobDetail(
    Guid JobId,
    Guid TenantId,
    Guid ServiceRequestId,
    string AssignmentStatus,
    Guid? AssignedWorkerUserId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string? RequestTitle,
    string? RequestStatus,
    IReadOnlyList<QueriedTimelineItem> Timeline);