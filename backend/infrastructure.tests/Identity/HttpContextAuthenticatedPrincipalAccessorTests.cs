using System.Security.Claims;
using GTEK.FSM.Backend.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Identity;

public class HttpContextAuthenticatedPrincipalAccessorTests
{
    [Fact]
    public void GetCurrent_WithValidClaims_ReturnsPrincipal()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var accessor = CreateAccessor(CreatePrincipal(
            new Claim("sub", userId.ToString()),
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("role", "Support"),
            new Claim("ver", "1")));

        var principal = accessor.GetCurrent();

        Assert.NotNull(principal);
        Assert.Equal(userId, principal!.UserId);
        Assert.Equal(tenantId, principal.TenantId);
        Assert.True(principal.IsInRole("support"));
    }

    [Fact]
    public void GetCurrent_WithUnauthenticatedContext_ReturnsNull()
    {
        var accessor = CreateAccessor(new ClaimsPrincipal(new ClaimsIdentity()));

        var principal = accessor.GetCurrent();

        Assert.Null(principal);
    }

    [Fact]
    public void GetCurrent_WithMissingTenantClaim_ReturnsNull()
    {
        var accessor = CreateAccessor(CreatePrincipal(
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim("role", "Support"),
            new Claim("ver", "1")));

        var principal = accessor.GetCurrent();

        Assert.Null(principal);
    }

    private static HttpContextAuthenticatedPrincipalAccessor CreateAccessor(ClaimsPrincipal principal)
    {
        var contextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal,
            },
        };

        return new HttpContextAuthenticatedPrincipalAccessor(contextAccessor);
    }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Bearer"));
    }
}
