namespace GTEK.FSM.Backend.Api.Tenancy;

public sealed class TenantResolutionOptions
{
    public const string SectionName = "Tenancy";

    public bool RequireTenantResolution { get; set; } = true;

    public string HeaderName { get; set; } = "X-Tenant-Id";

    public string[] HeaderFallbackAllowedRoles { get; set; } = new[] { "Admin" };
}
