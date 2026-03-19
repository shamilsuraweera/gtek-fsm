using GTEK.FSM.Backend.Application.Identity;
using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Identity;

public class AuthenticatedPrincipalTests
{
    [Fact]
    public void Constructor_WithValidIds_SetsPrincipalIdentity()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var principal = new AuthenticatedPrincipal(userId, tenantId);

        Assert.Equal(userId, principal.UserId);
        Assert.Equal(tenantId, principal.TenantId);
    }

    [Fact]
    public void Constructor_WithDuplicateRolesAndScopes_NormalizesDistinctValues()
    {
        var principal = new AuthenticatedPrincipal(
            Guid.NewGuid(),
            Guid.NewGuid(),
            roles: new[] { "Manager", "manager", " Support ", "" },
            scopes: new[] { "requests.read", "REQUESTS.READ", " jobs.write " });

        Assert.Equal(2, principal.Roles.Count);
        Assert.Equal(2, principal.Scopes.Count);
        Assert.True(principal.IsInRole("support"));
        Assert.True(principal.HasScope("jobs.write"));
    }

    [Fact]
    public void IsInRole_And_HasScope_AreCaseInsensitive()
    {
        var principal = new AuthenticatedPrincipal(
            Guid.NewGuid(),
            Guid.NewGuid(),
            roles: new[] { "Admin" },
            scopes: new[] { "tenants.manage" });

        Assert.True(principal.IsInRole("admin"));
        Assert.True(principal.HasScope("TENANTS.MANAGE"));
    }

    [Fact]
    public void Constructor_WithEmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new AuthenticatedPrincipal(Guid.Empty, Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_WithEmptyTenantId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new AuthenticatedPrincipal(Guid.NewGuid(), Guid.Empty));
    }
}
