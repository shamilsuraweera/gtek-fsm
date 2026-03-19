namespace GTEK.FSM.Backend.Application.Persistence.Specifications;

public enum UserSortField
{
    DisplayName = 0,
    CreatedAtUtc = 1,
}

public sealed record UserQuerySpecification(
    Guid TenantId,
    string? SearchText = null,
    string? ExternalIdentity = null,
    PageSpecification? Page = null,
    UserSortField SortBy = UserSortField.DisplayName,
    SortDirection SortDirection = SortDirection.Ascending);
