namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Categories.Requests;

public sealed class UpdateCategoryRequest
{
    public string? Code { get; set; }

    public string? Name { get; set; }

    public int? SortOrder { get; set; }

    public bool? IsEnabled { get; set; }
}
