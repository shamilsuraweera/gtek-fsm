using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Categories.Requests;

namespace GTEK.FSM.Backend.Application.Categories;

public interface ICategoryManagementService
{
    Task<CategoryMutationResult> CreateAsync(
        AuthenticatedPrincipal principal,
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<CategoryMutationResult> UpdateAsync(
        AuthenticatedPrincipal principal,
        Guid categoryId,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<CategoryMutationResult> DisableAsync(
        AuthenticatedPrincipal principal,
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<CategoriesQueryResult> ReorderAsync(
        AuthenticatedPrincipal principal,
        ReorderCategoriesRequest request,
        CancellationToken cancellationToken = default);
}
