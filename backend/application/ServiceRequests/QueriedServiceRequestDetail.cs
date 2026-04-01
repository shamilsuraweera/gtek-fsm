namespace GTEK.FSM.Backend.Application.ServiceRequests;

/// <summary>
/// Represents a tenant-scoped service request detail payload.
/// </summary>
public sealed record QueriedServiceRequestDetail(
    Guid RequestId,
    string? RowVersion,
    Guid TenantId,
    Guid CustomerUserId,
    string Title,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    Guid? ActiveJobId,
    Guid? AssignedWorkerUserId,
    string? ActiveJobStatus,
    IReadOnlyList<QueriedTimelineItem> Timeline);