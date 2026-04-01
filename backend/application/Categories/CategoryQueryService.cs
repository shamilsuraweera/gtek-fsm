using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Persistence.Repositories;

namespace GTEK.FSM.Backend.Application.Categories;

internal sealed class CategoryQueryService : ICategoryQueryService
{
    private readonly ICategoryRepository categoryRepository;

    public CategoryQueryService(ICategoryRepository categoryRepository)
    {
        this.categoryRepository = categoryRepository;
    }

    public async Task<CategoriesQueryResult> GetCategoriesAsync(
        AuthenticatedPrincipal principal,
        bool includeDisabled,
        CancellationToken cancellationToken = default)
    {
        if (!IsAllowedReadRole(principal))
        {
            return CategoriesQueryResult.Failure(
                "Role is not authorized to query categories.",
                "AUTH_FORBIDDEN_ROLE",
                403);
        }

        var categories = includeDisabled
            ? await this.categoryRepository.ListByTenantAsync(principal.TenantId, includeDisabled: true, cancellationToken)
            : await this.categoryRepository.ListActiveByTenantAsync(principal.TenantId, cancellationToken);

        var payload = categories
            .Select(x => new QueriedCategoryItem(
                CategoryId: x.Id,
                TenantId: x.TenantId,
                Code: x.Code,
                Name: x.Name,
                SortOrder: x.SortOrder,
                IsEnabled: x.IsEnabled,
                CreatedAtUtc: x.CreatedAtUtc,
                UpdatedAtUtc: x.UpdatedAtUtc))
            .ToArray();

        return CategoriesQueryResult.Success(payload);
    }

    private static bool IsAllowedReadRole(AuthenticatedPrincipal principal)
    {
        return principal.IsInRole("Customer")
            || principal.IsInRole("Worker")
            || principal.IsInRole("Support")
            || principal.IsInRole("Manager")
            || principal.IsInRole("Admin");
    }
}
