using System.Security.Claims;

using GTEK.FSM.Backend.Api.Middleware;
using GTEK.FSM.Backend.Api.Tenancy;
using GTEK.FSM.Backend.Application.Identity;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Runtime;

public class TenantContextRequiredRuntimeTests
{
    [Fact]
    public async Task InvokeAsync_AuthenticatedRequestWithoutTenantContext_BlocksRequestWith401()
    {
        var nextCalled = false;

        var middleware = new TenantResolutionMiddleware(
            next: _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            options: Options.Create(new TenantResolutionOptions
            {
                RequireTenantResolution = true,
                HeaderName = "X-Tenant-Id",
                HeaderFallbackAllowedRoles = new[] { "Admin" },
            }));

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/tenant/00000000-0000-0000-0000-000000000001/ownership-check/read";
        context.Response.Body = new MemoryStream();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            claims: new[]
            {
                new Claim(TokenClaimNames.Subject, Guid.NewGuid().ToString()),
                new Claim(TokenClaimNames.Role, "Support"),
                new Claim(TokenClaimNames.TokenVersion, "1"),
            },
            authenticationType: "Bearer"));

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();

        Assert.Contains("TENANT_CONTEXT_UNRESOLVED", body);
    }
}
