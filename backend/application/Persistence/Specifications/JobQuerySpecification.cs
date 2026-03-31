using GTEK.FSM.Backend.Domain.Enums;

namespace GTEK.FSM.Backend.Application.Persistence.Specifications;

public enum JobSortField
{
    CreatedAtUtc = 0,
    AssignmentStatus = 1,
}

public sealed record JobQuerySpecification(
    Guid TenantId,
    Guid? ServiceRequestId = null,
    Guid? AssignedWorkerUserId = null,
    AssignmentStatus? AssignmentStatus = null,
    DateTime? ScheduledFromUtc = null,
    DateTime? ScheduledToUtc = null,
    string? SearchText = null,
    PageSpecification? Page = null,
    JobSortField SortBy = JobSortField.CreatedAtUtc,
    SortDirection SortDirection = SortDirection.Descending);
