namespace GTEK.FSM.Backend.Application.Persistence.Specifications;

public enum WorkerProfileSortField
{
    DisplayName = 0,
    CreatedAtUtc = 1,
    InternalRating = 2,
}

public sealed record WorkerProfileQuerySpecification(
    Guid TenantId,
    string? SearchText = null,
    bool IncludeInactive = false,
    PageSpecification? Page = null,
    WorkerProfileSortField SortBy = WorkerProfileSortField.DisplayName,
    SortDirection SortDirection = SortDirection.Ascending);
