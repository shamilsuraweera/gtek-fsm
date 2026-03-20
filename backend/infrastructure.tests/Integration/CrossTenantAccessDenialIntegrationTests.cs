using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;

using GTEK.FSM.Backend.Api.Authorization;
using GTEK.FSM.Backend.Api.Middleware;
using GTEK.FSM.Backend.Api.Routing;
using GTEK.FSM.Backend.Api.Tenancy;
using GTEK.FSM.Backend.Application;
using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Infrastructure.Identity;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Integration;

public class CrossTenantAccessDenialIntegrationTests
{
    [Fact]
    public async Task CustomerCrossTenantRead_IsForbidden_WithTenantOwnershipMismatch()
    {
        var app = await BuildTestApplicationAsync();
        using var client = app.GetTestClient();

        var claimTenant = Guid.NewGuid();
        var targetTenant = Guid.NewGuid();

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Get,
            $"/api/v1/tenant/{targetTenant}/ownership-check/read",
            role: "Customer",
            tenantId: claimTenant);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("TENANT_OWNERSHIP_MISMATCH", body);
    }

    [Fact]
    public async Task WorkerCrossTenantWrite_IsForbidden_WithTenantOwnershipMismatch()
    {
        var app = await BuildTestApplicationAsync();
        using var client = app.GetTestClient();

        var claimTenant = Guid.NewGuid();
        var targetTenant = Guid.NewGuid();

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/tenant/{targetTenant}/ownership-check/write",
            role: "Worker",
            tenantId: claimTenant);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("TENANT_OWNERSHIP_MISMATCH", body);
    }

    [Fact]
    public async Task ManagerCrossTenantManagementProbe_IsForbidden_WithCrossTenantForbiddenCode()
    {
        var app = await BuildTestApplicationAsync();
        using var client = app.GetTestClient();

        var claimTenant = Guid.NewGuid();
        var targetTenant = Guid.NewGuid();

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/management/cross-tenant/{targetTenant}/guarded-probe",
            role: "Manager",
            tenantId: claimTenant);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("CROSS_TENANT_FORBIDDEN", body);
    }

    [Fact]
    public async Task AdminCrossTenantManagementProbe_IsAllowed_AsPrivilegedFlowException()
    {
        var app = await BuildTestApplicationAsync();
        using var client = app.GetTestClient();

        var claimTenant = Guid.NewGuid();
        var targetTenant = Guid.NewGuid();

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/management/cross-tenant/{targetTenant}/guarded-probe",
            role: "Admin",
            tenantId: claimTenant);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ManagerSameTenantManagementProbe_IsAllowed()
    {
        var app = await BuildTestApplicationAsync();
        using var client = app.GetTestClient();

        var tenantId = Guid.NewGuid();

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/management/cross-tenant/{tenantId}/guarded-probe",
            role: "Manager",
            tenantId: tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string route, string role, Guid tenantId)
    {
        var request = new HttpRequestMessage(method, route);
        request.Headers.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName, "ok");
        request.Headers.Add(TestAuthHeaders.Subject, Guid.NewGuid().ToString());
        request.Headers.Add(TestAuthHeaders.TenantId, tenantId.ToString());
        request.Headers.Add(TestAuthHeaders.Role, role);
        request.Headers.Add(TestAuthHeaders.TokenVersion, "1");
        return request;
    }

    private static async Task<WebApplication> BuildTestApplicationAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddApplication();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IAuthenticatedPrincipalAccessor, HttpContextAuthenticatedPrincipalAccessor>();
        builder.Services.AddScoped<ITenantContextAccessor, HttpContextTenantContextAccessor>();

        builder.Services.Configure<TenantResolutionOptions>(_ => { });

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultForbidScheme = TestAuthHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

        builder.Services.AddApiAuthorizationPolicies();

        var app = builder.Build();

        app.UseGlobalExceptionHandling();
        app.UseAuthentication();
        app.UseTenantResolution();
        app.UseAuthorization();
        app.MapV1Endpoints();

        await app.StartAsync();
        return app;
    }

    private static class TestAuthHeaders
    {
        public const string Subject = "X-Test-Sub";
        public const string TenantId = "X-Test-Tenant-Id";
        public const string Role = "X-Test-Role";
        public const string TokenVersion = "X-Test-Ver";
    }

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "Test";

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("Authorization", out var authValues)
                || authValues.Count == 0)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim>();

            if (TryGetHeader(TestAuthHeaders.Subject, out var subject))
            {
                claims.Add(new Claim(TokenClaimNames.Subject, subject));
            }

            if (TryGetHeader(TestAuthHeaders.TenantId, out var tenantId))
            {
                claims.Add(new Claim(TokenClaimNames.TenantId, tenantId));
            }

            if (TryGetHeader(TestAuthHeaders.Role, out var role))
            {
                claims.Add(new Claim(TokenClaimNames.Role, role));
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            if (TryGetHeader(TestAuthHeaders.TokenVersion, out var tokenVersion))
            {
                claims.Add(new Claim(TokenClaimNames.TokenVersion, tokenVersion));
            }

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        private bool TryGetHeader(string key, out string value)
        {
            value = string.Empty;
            if (!Request.Headers.TryGetValue(key, out var values) || values.Count == 0)
            {
                return false;
            }

            value = values[0] ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value);
        }
    }
}
