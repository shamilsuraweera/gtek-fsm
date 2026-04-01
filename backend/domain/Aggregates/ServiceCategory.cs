using GTEK.FSM.Backend.Domain.Rules;

namespace GTEK.FSM.Backend.Domain.Aggregates;

/// <summary>
/// Service category aggregate for tenant-scoped request taxonomy.
/// </summary>
public sealed class ServiceCategory
{
    public ServiceCategory(Guid id, Guid tenantId, string code, string name, int sortOrder = 0)
    {
        this.Id = DomainGuards.RequiredId(id, nameof(id), "Category id cannot be empty.");
        this.TenantId = DomainGuards.RequiredId(tenantId, nameof(tenantId), "Category must belong to a tenant.");
        this.Code = NormalizeCode(code);
        this.Name = DomainGuards.RequiredText(name, nameof(name), "Category name is required.", 120);
        this.SortOrder = NormalizeSortOrder(sortOrder);
        this.IsEnabled = true;
    }

    public Guid Id { get; }

    public Guid TenantId { get; }

    public string Code { get; private set; }

    public string Name { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsEnabled { get; private set; }

    public DateTime CreatedAtUtc { get; internal set; }

    public DateTime UpdatedAtUtc { get; internal set; }

    public bool IsDeleted { get; internal set; }

    public void Update(string code, string name, int sortOrder)
    {
        this.Code = NormalizeCode(code);
        this.Name = DomainGuards.RequiredText(name, nameof(name), "Category name is required.", 120);
        this.SortOrder = NormalizeSortOrder(sortOrder);
    }

    public void Disable()
    {
        this.IsEnabled = false;
    }

    public void Enable()
    {
        this.IsEnabled = true;
    }

    public void Reorder(int sortOrder)
    {
        this.SortOrder = NormalizeSortOrder(sortOrder);
    }

    private static string NormalizeCode(string code)
    {
        var normalized = DomainGuards.RequiredText(code, nameof(code), "Category code is required.", 32)
            .Trim()
            .ToUpperInvariant();

        return normalized;
    }

    private static int NormalizeSortOrder(int sortOrder)
    {
        if (sortOrder < 0 || sortOrder > 10000)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder), "sortOrder must be between 0 and 10000.");
        }

        return sortOrder;
    }
}