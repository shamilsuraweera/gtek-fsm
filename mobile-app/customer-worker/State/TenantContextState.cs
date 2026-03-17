namespace GTEK.FSM.MobileApp.State;

public sealed class TenantContextState
{
    public string TenantId { get; private set; } = string.Empty;

    public string TenantName { get; private set; } = string.Empty;

    public bool HasTenantContext => !string.IsNullOrWhiteSpace(TenantId);

    public DateTimeOffset? LastUpdatedUtc { get; private set; }

    public void Update(string tenantId, string tenantName)
    {
        TenantId = tenantId;
        TenantName = tenantName;
        LastUpdatedUtc = DateTimeOffset.UtcNow;
    }

    public void Clear()
    {
        TenantId = string.Empty;
        TenantName = string.Empty;
        LastUpdatedUtc = DateTimeOffset.UtcNow;
    }
}
