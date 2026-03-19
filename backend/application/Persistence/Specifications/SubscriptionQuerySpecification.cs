namespace GTEK.FSM.Backend.Application.Persistence.Specifications;

public enum SubscriptionSortField
{
    StartsOnUtc = 0,
    EndsOnUtc = 1,
    PlanCode = 2,
}

public sealed record SubscriptionQuerySpecification(
    Guid TenantId,
    bool ActiveOnly = false,
    string? PlanCode = null,
    PageSpecification? Page = null,
    SubscriptionSortField SortBy = SubscriptionSortField.StartsOnUtc,
    SortDirection SortDirection = SortDirection.Descending);
