using GTEK.FSM.Backend.Domain.Enums;

namespace GTEK.FSM.Backend.Application.Persistence.Specifications;

public enum ServiceRequestSortField
{
    CreatedAtUtc = 0,
    Status = 1,
    Title = 2,
}

public sealed record ServiceRequestQuerySpecification(
    Guid TenantId,
    Guid? CustomerUserId = null,
    ServiceRequestStatus? Status = null,
    DateTime? CreatedFromUtc = null,
    DateTime? CreatedToUtc = null,
    Guid? AssignedWorkerUserId = null,
    string? SearchText = null,
    PageSpecification? Page = null,
    ServiceRequestSortField SortBy = ServiceRequestSortField.CreatedAtUtc,
    SortDirection SortDirection = SortDirection.Descending);
