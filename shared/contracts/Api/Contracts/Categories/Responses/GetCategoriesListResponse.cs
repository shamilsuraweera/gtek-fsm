namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Categories.Responses;

public sealed class GetCategoriesListResponse
{
    public IReadOnlyList<CategoryResponse> Items { get; set; } = Array.Empty<CategoryResponse>();
}
