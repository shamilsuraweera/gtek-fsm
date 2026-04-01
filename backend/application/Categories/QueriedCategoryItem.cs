namespace GTEK.FSM.Backend.Application.Categories;

public sealed record QueriedCategoryItem(
    Guid CategoryId,
    Guid TenantId,
    string Code,
    string Name,
    int SortOrder,
    bool IsEnabled,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
