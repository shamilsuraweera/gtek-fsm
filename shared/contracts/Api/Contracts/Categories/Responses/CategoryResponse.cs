namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Categories.Responses;

public sealed class CategoryResponse
{
    public string? CategoryId { get; set; }

    public string? TenantId { get; set; }

    public string? Code { get; set; }

    public string? Name { get; set; }

    public int SortOrder { get; set; }

    public bool IsEnabled { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
