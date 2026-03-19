using GTEK.FSM.Backend.Application.Identity;

using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Identity;

public class TenantResolutionPolicyTests
{
    [Fact]
    public void Resolve_WithValidClaim_PrefersClaimOverHeader()
    {
        var claimTenant = Guid.NewGuid();
        var headerTenant = Guid.NewGuid();

        var result = TenantResolutionPolicy.Resolve(new TenantResolutionInput(
            TenantClaimValue: claimTenant.ToString(),
            TenantHeaderValue: headerTenant.ToString(),
            AllowHeaderFallback: true));

        Assert.True(result.IsSuccess);
        Assert.Equal(claimTenant, result.TenantId);
    }

    [Fact]
    public void Resolve_WithMissingClaimAndAllowedFallback_UsesHeader()
    {
        var headerTenant = Guid.NewGuid();

        var result = TenantResolutionPolicy.Resolve(new TenantResolutionInput(
            TenantClaimValue: null,
            TenantHeaderValue: headerTenant.ToString(),
            AllowHeaderFallback: true));

        Assert.True(result.IsSuccess);
        Assert.Equal(headerTenant, result.TenantId);
    }

    [Fact]
    public void Resolve_WithMissingClaimAndMissingHeader_ReturnsUnresolved()
    {
        var result = TenantResolutionPolicy.Resolve(new TenantResolutionInput(
            TenantClaimValue: null,
            TenantHeaderValue: null,
            AllowHeaderFallback: false));

        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.StatusCode);
        Assert.Equal("TENANT_CONTEXT_UNRESOLVED", result.ErrorCode);
    }

    [Fact]
    public void Resolve_WithHeaderButFallbackDisallowed_ReturnsForbidden()
    {
        var result = TenantResolutionPolicy.Resolve(new TenantResolutionInput(
            TenantClaimValue: null,
            TenantHeaderValue: Guid.NewGuid().ToString(),
            AllowHeaderFallback: false));

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.StatusCode);
        Assert.Equal("TENANT_HEADER_FALLBACK_NOT_ALLOWED", result.ErrorCode);
    }

    [Fact]
    public void Resolve_WithMalformedClaim_ReturnsUnauthorized()
    {
        var result = TenantResolutionPolicy.Resolve(new TenantResolutionInput(
            TenantClaimValue: "not-a-guid",
            TenantHeaderValue: null,
            AllowHeaderFallback: true));

        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.StatusCode);
        Assert.Equal("MALFORMED_TENANT_CLAIM", result.ErrorCode);
    }

    [Fact]
    public void Resolve_WithMalformedHeader_ReturnsBadRequest()
    {
        var result = TenantResolutionPolicy.Resolve(new TenantResolutionInput(
            TenantClaimValue: null,
            TenantHeaderValue: "invalid",
            AllowHeaderFallback: true));

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("MALFORMED_TENANT_HEADER", result.ErrorCode);
    }
}
