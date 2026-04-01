namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Categories.Requests;

public sealed class ReorderCategoriesRequest
{
    public IReadOnlyList<ReorderCategoryItemRequest> Items { get; set; } = Array.Empty<ReorderCategoryItemRequest>();
}

public sealed class ReorderCategoryItemRequest
{
    public string? CategoryId { get; set; }

    public int? SortOrder { get; set; }
}
