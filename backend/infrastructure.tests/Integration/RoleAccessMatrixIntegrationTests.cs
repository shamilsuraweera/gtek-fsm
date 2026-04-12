using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;

using GTEK.FSM.Backend.Api.Authentication;
using GTEK.FSM.Backend.Api.Authorization;
using GTEK.FSM.Backend.Api.Middleware;
using GTEK.FSM.Backend.Api.Routing;
using GTEK.FSM.Backend.Api.Tenancy;
using GTEK.FSM.Backend.Application;
using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Infrastructure.Identity;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Auth.Requests;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Integration;

public class RoleAccessMatrixIntegrationTests
{
    [Theory]
    [InlineData("Guest", "GET", "/api/v1/auth/bootstrap/authenticated")]
    [InlineData("Customer", "GET", "/api/v1/auth/bootstrap/authenticated")]
    [InlineData("Worker", "GET", "/api/v1/auth/bootstrap/authenticated")]
    [InlineData("Support", "GET", "/api/v1/auth/bootstrap/authenticated")]
    [InlineData("Manager", "GET", "/api/v1/auth/bootstrap/authenticated")]
    [InlineData("Admin", "GET", "/api/v1/auth/bootstrap/authenticated")]
    [InlineData("Admin", "GET", "/api/v1/auth/bootstrap/forbidden")]
    [InlineData("Customer", "GET", "/api/v1/tenant/{tenantId}/ownership-check/read")]
    [InlineData("Support", "GET", "/api/v1/tenant/{tenantId}/ownership-check/read")]
    [InlineData("Manager", "GET", "/api/v1/tenant/{tenantId}/ownership-check/read")]
    [InlineData("Admin", "GET", "/api/v1/tenant/{tenantId}/ownership-check/read")]
    [InlineData("Worker", "POST", "/api/v1/tenant/{tenantId}/ownership-check/write")]
    [InlineData("Support", "POST", "/api/v1/tenant/{tenantId}/ownership-check/write")]
    [InlineData("Manager", "POST", "/api/v1/tenant/{tenantId}/ownership-check/write")]
    [InlineData("Admin", "POST", "/api/v1/tenant/{tenantId}/ownership-check/write")]
    [InlineData("Manager", "POST", "/api/v1/management/cross-tenant/{tenantId}/guarded-probe")]
    [InlineData("Admin", "POST", "/api/v1/management/cross-tenant/{tenantId}/guarded-probe")]
    public async Task CriticalPhase2Endpoints_AllowedRoles_ReturnSuccess(string role, string method, string routeTemplate)
    {
        await using var app = await BuildTestApplicationAsync();
        using var client = app.GetTestClient();

        var tenantId = Guid.NewGuid();
        var route = routeTemplate.Replace("{tenantId}", tenantId.ToString(), StringComparison.Ordinal);
        using var request = CreateAuthenticatedRequest(method, route, role, tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("Guest", "GET", "/api/v1/auth/bootstrap/forbidden")]
    [InlineData("Customer", "GET", "/api/v1/auth/bootstrap/forbidden")]
    [InlineData("Worker", "GET", "/api/v1/auth/bootstrap/forbidden")]
    [InlineData("Support", "GET", "/api/v1/auth/bootstrap/forbidden")]
    [InlineData("Manager", "GET", "/api/v1/auth/bootstrap/forbidden")]
    [InlineData("Guest", "GET", "/api/v1/tenant/{tenantId}/ownership-check/read")]
    [InlineData("Worker", "GET", "/api/v1/tenant/{tenantId}/ownership-check/read")]
    [InlineData("Guest", "POST", "/api/v1/tenant/{tenantId}/ownership-check/write")]
    [InlineData("Customer", "POST", "/api/v1/tenant/{tenantId}/ownership-check/write")]
    [InlineData("Guest", "POST", "/api/v1/management/cross-tenant/{tenantId}/guarded-probe")]
    [InlineData("Customer", "POST", "/api/v1/management/cross-tenant/{tenantId}/guarded-probe")]
    [InlineData("Worker", "POST", "/api/v1/management/cross-tenant/{tenantId}/guarded-probe")]
    [InlineData("Support", "POST", "/api/v1/management/cross-tenant/{tenantId}/guarded-probe")]
    public async Task CriticalPhase2Endpoints_DisallowedRoles_ReturnForbidden(string role, string method, string routeTemplate)
    {
        await using var app = await BuildTestApplicationAsync();
        using var client = app.GetTestClient();

        var tenantId = Guid.NewGuid();
        var route = routeTemplate.Replace("{tenantId}", tenantId.ToString(), StringComparison.Ordinal);
        using var request = CreateAuthenticatedRequest(method, route, role, tenantId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static HttpRequestMessage CreateAuthenticatedRequest(string method, string route, string role, Guid tenantId)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), route);
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
        builder.Services.AddSingleton<ILocalAuthService, StubLocalAuthService>();

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

    private sealed class StubLocalAuthService : ILocalAuthService
    {
        public Task<LocalAuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(LocalAuthResult.Fail(401, "STUB", "stub"));

        public Task<LocalAuthResult> RegisterAsync(RegisterLocalUserRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(LocalAuthResult.Fail(401, "STUB", "stub"));
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
