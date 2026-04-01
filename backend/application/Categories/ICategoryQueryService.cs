using GTEK.FSM.Backend.Application.Identity;

namespace GTEK.FSM.Backend.Application.Categories;

public interface ICategoryQueryService
{
    Task<CategoriesQueryResult> GetCategoriesAsync(
        AuthenticatedPrincipal principal,
        bool includeDisabled,
        CancellationToken cancellationToken = default);
}
