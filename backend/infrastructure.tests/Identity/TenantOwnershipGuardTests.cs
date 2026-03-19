using GTEK.FSM.Backend.Application.Identity;

using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Identity;

public class TenantOwnershipGuardTests
{
    [Fact]
    public void EnsureTenantAccess_WhenPrincipalAndResolvedTenantMatchRequested_Allows()
    {
        var tenantId = Guid.NewGuid();
        var guard = CreateGuard(
            principal: new AuthenticatedPrincipal(Guid.NewGuid(), tenantId, new[] { "Support" }, null),
            resolvedTenant: tenantId);

        var result = guard.EnsureTenantAccess(tenantId);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void EnsureTenantAccess_WhenPrincipalMissing_ReturnsUnauthorized()
    {
        var guard = CreateGuard(principal: null, resolvedTenant: Guid.NewGuid());

        var result = guard.EnsureTenantAccess(Guid.NewGuid());

        Assert.False(result.IsAllowed);
        Assert.Equal(401, result.StatusCode);
        Assert.Equal("AUTH_UNAUTHORIZED", result.ErrorCode);
    }

    [Fact]
    public void EnsureTenantAccess_WhenPrincipalTenantDiffers_ReturnsForbidden()
    {
        var requestedTenant = Guid.NewGuid();
        var guard = CreateGuard(
            principal: new AuthenticatedPrincipal(Guid.NewGuid(), Guid.NewGuid(), new[] { "Support" }, null),
            resolvedTenant: requestedTenant);

        var result = guard.EnsureTenantAccess(requestedTenant);

        Assert.False(result.IsAllowed);
        Assert.Equal(403, result.StatusCode);
        Assert.Equal("TENANT_OWNERSHIP_MISMATCH", result.ErrorCode);
    }

    [Fact]
    public void EnsureTenantAccess_WhenResolvedTenantDiffers_ReturnsForbidden()
    {
        var requestedTenant = Guid.NewGuid();
        var guard = CreateGuard(
            principal: new AuthenticatedPrincipal(Guid.NewGuid(), requestedTenant, new[] { "Support" }, null),
            resolvedTenant: Guid.NewGuid());

        var result = guard.EnsureTenantAccess(requestedTenant);

        Assert.False(result.IsAllowed);
        Assert.Equal(403, result.StatusCode);
        Assert.Equal("TENANT_CONTEXT_MISMATCH", result.ErrorCode);
    }

    private static ITenantOwnershipGuard CreateGuard(AuthenticatedPrincipal? principal, Guid? resolvedTenant)
    {
        return new TenantOwnershipGuard(
            new StubPrincipalAccessor(principal),
            new StubTenantContextAccessor(resolvedTenant));
    }

    private sealed class StubPrincipalAccessor(AuthenticatedPrincipal? principal) : IAuthenticatedPrincipalAccessor
    {
        public AuthenticatedPrincipal? GetCurrent() => principal;
    }

    private sealed class StubTenantContextAccessor(Guid? tenantId) : ITenantContextAccessor
    {
        public Guid? GetCurrentTenantId() => tenantId;
    }
}
