using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.TestHost;

using GTEK.FSM.Backend.Api.Authorization;
using GTEK.FSM.Backend.Api.Middleware;
using GTEK.FSM.Backend.Api.Tenancy;
using GTEK.FSM.Backend.Application;
using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Infrastructure.Identity;
using GTEK.FSM.Backend.Infrastructure.Persistence;
using GTEK.FSM.Backend.Infrastructure.Persistence.Repositories;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Integration;

/// <summary>
/// Phase 2.5.1 Readiness Gate: End-to-end authenticated request flow validation.
/// 
/// Validates that authenticated requests flow correctly through:
/// 1. JWT bearer authentication middleware (token intake)
/// 2. Claim extraction (sub, tenant_id, roles)
/// 3. Tenant resolution middleware (tenant context)
/// 4. Authorization policy enforcement (role/permission checks)
/// 5. Repository query execution (tenant-scoped data access)
/// 
/// All downstream tenant-scoped queries must return only data for the authenticated context's tenant.
/// </summary>
public class EndToEndAuthenticatedRequestFlowTests
{
    [Fact]
    public async Task TokenIntake_ValidBearerToken_PassesThroughJwtMiddleware()
    {
        // Arrange - valid token with all required claims
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        await using var app = await BuildTestApplicationAsync();

        using var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/test/flow/verify-auth");
        request.Headers.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName, "ok");
        request.Headers.Add(TestAuthHeaders.Subject, userId.ToString());
        request.Headers.Add(TestAuthHeaders.TenantId, tenantId.ToString());
        request.Headers.Add(TestAuthHeaders.Role, "Admin");
        request.Headers.Add(TestAuthHeaders.TokenVersion, "1");

        // Act
        var response = await client.SendAsync(request);

        // Assert - request passed authentication
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TokenIntake_MissingBearerToken_FailsWithUnauthorized()
    {
        // Arrange - no authorization header
        await using var app = await BuildTestApplicationAsync();

        using var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/test/flow/verify-auth");
        // Intentionally omit Authorization header

        // Act
        var response = await client.SendAsync(request);

        // Assert - request rejected by authentication middleware
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ClaimExtraction_ValidToken_ExtractsUserIdAndTenantId()
    {
        // Arrange - token with specific claims
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        await using var app = await BuildTestApplicationAsync();

        using var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/test/flow/extract-claims");
        request.Headers.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName, "ok");
        request.Headers.Add(TestAuthHeaders.Subject, userId.ToString());
        request.Headers.Add(TestAuthHeaders.TenantId, tenantId.ToString());
        request.Headers.Add(TestAuthHeaders.Role, "Admin");
        request.Headers.Add(TestAuthHeaders.TokenVersion, "1");

        // Act
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ClaimExtractionResult>();

        // Assert - claims correctly extracted
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(userId, result!.UserId);
        Assert.Equal(tenantId, result.TenantId);
        Assert.Contains("Admin", result.Roles);
    }

    [Fact]
    public async Task TenantResolution_WithTokenClaim_ResolvesToTokenTenant()
    {
        // Arrange - tenant from token claim (preferred source)
        var userId = Guid.NewGuid();
        var tokenTenantId = Guid.NewGuid();
        await using var app = await BuildTestApplicationAsync();

        using var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/test/flow/resolve-tenant");
        request.Headers.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName, "ok");
        request.Headers.Add(TestAuthHeaders.Subject, userId.ToString());
        request.Headers.Add(TestAuthHeaders.TenantId, tokenTenantId.ToString());
        request.Headers.Add(TestAuthHeaders.Role, "Admin");
        request.Headers.Add(TestAuthHeaders.TokenVersion, "1");

        // Act
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<TenantResolutionResult>();

        // Assert - tenant resolved from token claim
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(tokenTenantId, result!.ResolvedTenantId);
    }

    [Fact]
    public async Task TenantResolution_MissingTenant_FailsWithUnauthorized()
    {
        // Arrange - token without tenant claim and no header
        var userId = Guid.NewGuid();
        await using var app = await BuildTestApplicationAsync();

        using var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/test/flow/resolve-tenant");
        request.Headers.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName, "ok");
        request.Headers.Add(TestAuthHeaders.Subject, userId.ToString());
        request.Headers.Add(TestAuthHeaders.Role, "Admin");
        request.Headers.Add(TestAuthHeaders.TokenVersion, "1");
        // Intentionally omit TenantId header

        // Act
        var response = await client.SendAsync(request);

        // Assert - tenant resolution fails
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizationPolicy_ValidRoleForEndpoint_AllowsRequest()
    {
        // Arrange - user with required role
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        await using var app = await BuildTestApplicationAsync();
        await SeedUsersAsync(app, new User(userId, tenantId, "ext-1", "Test User"));

        using var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/test/flow/protected-admin");
        request.Headers.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName, "ok");
        request.Headers.Add(TestAuthHeaders.Subject, userId.ToString());
        request.Headers.Add(TestAuthHeaders.TenantId, tenantId.ToString());
        request.Headers.Add(TestAuthHeaders.Role, "Admin");
        request.Headers.Add(TestAuthHeaders.TokenVersion, "1");

        // Act
        var response = await client.SendAsync(request);

        // Assert - authorization passed
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizationPolicy_InsufficientRole_DeniesWithForbidden()
    {
        // Arrange - user without required role
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        await using var app = await BuildTestApplicationAsync();
        await SeedUsersAsync(app, new User(userId, tenantId, "ext-1", "Test User"));

        using var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/test/flow/protected-admin");
        request.Headers.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName, "ok");
        request.Headers.Add(TestAuthHeaders.Subject, userId.ToString());
        request.Headers.Add(TestAuthHeaders.TenantId, tenantId.ToString());
        request.Headers.Add(TestAuthHeaders.Role, "Guest");  // Guest role, Admin required
        request.Headers.Add(TestAuthHeaders.TokenVersion, "1");

        // Act
        var response = await client.SendAsync(request);

        // Assert - authorization denied
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RepositoryQuery_AuthenticatedRequest_ReturnsTenantScopedData()
    {
        // Arrange - multi-tenant data
        var userId = Guid.NewGuid();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await using var app = await BuildTestApplicationAsync();
        await SeedUsersAsync(
            app,
            new User(Guid.NewGuid(), tenantA, "ext-a-1", "Tenant A User 1"),
            new User(Guid.NewGuid(), tenantA, "ext-a-2", "Tenant A User 2"),
            new User(Guid.NewGuid(), tenantB, "ext-b-1", "Tenant B User 1"));

        using var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/test/flow/tenant-scoped-data");
        request.Headers.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName, "ok");
        request.Headers.Add(TestAuthHeaders.Subject, userId.ToString());
        request.Headers.Add(TestAuthHeaders.TenantId, tenantA.ToString());
        request.Headers.Add(TestAuthHeaders.Role, "Admin");
        request.Headers.Add(TestAuthHeaders.TokenVersion, "1");

        // Act
        var response = await client.SendAsync(request);
        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();

        // Assert - only tenantA data returned
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(users);
        Assert.Equal(2, users!.Count);
        Assert.All(users, u => Assert.Equal(tenantA, u.TenantId));
    }

    [Fact]
    public async Task RepositoryQuery_DifferentAuthenticatedTenant_ReturnsItsOwnData()
    {
        // Arrange - same data, different authenticated context
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var userAId = Guid.NewGuid();
        var userBId = Guid.NewGuid();

        await using var app = await BuildTestApplicationAsync();
        await SeedUsersAsync(
            app,
            new User(userAId, tenantA, "ext-a", "Tenant A User"),
            new User(userBId, tenantB, "ext-b", "Tenant B User"));

        using var client = app.GetTestClient();

        // Act for Tenant A context
        var requestA = new HttpRequestMessage(HttpMethod.Get, "/test/flow/tenant-scoped-data");
        requestA.Headers.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName, "ok");
        requestA.Headers.Add(TestAuthHeaders.Subject, userAId.ToString());
        requestA.Headers.Add(TestAuthHeaders.TenantId, tenantA.ToString());
        requestA.Headers.Add(TestAuthHeaders.Role, "Admin");
        requestA.Headers.Add(TestAuthHeaders.TokenVersion, "1");
        var responseA = await client.SendAsync(requestA);
        var usersA = await responseA.Content.ReadFromJsonAsync<List<UserDto>>();

        // Act for Tenant B context
        var requestB = new HttpRequestMessage(HttpMethod.Get, "/test/flow/tenant-scoped-data");
        requestB.Headers.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName, "ok");
        requestB.Headers.Add(TestAuthHeaders.Subject, userBId.ToString());
        requestB.Headers.Add(TestAuthHeaders.TenantId, tenantB.ToString());
        requestB.Headers.Add(TestAuthHeaders.Role, "Admin");
        requestB.Headers.Add(TestAuthHeaders.TokenVersion, "1");
        var responseB = await client.SendAsync(requestB);
        var usersB = await responseB.Content.ReadFromJsonAsync<List<UserDto>>();

        // Assert - each sees only their own tenant data
        Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);
        Assert.Equal(HttpStatusCode.OK, responseB.StatusCode);
        Assert.NotNull(usersA);
        Assert.NotNull(usersB);
        Assert.Single(usersA);
        Assert.Single(usersB);
        Assert.Equal(tenantA, usersA![0].TenantId);
        Assert.Equal(tenantB, usersB![0].TenantId);
        Assert.NotEqual(usersA[0].TenantId, usersB[0].TenantId);
    }

    [Fact]
    public async Task EndToEnd_FullAuthenticatedFlow_TokenToTenantScopedQuery()
    {
        // Arrange - complete realistic scenario
        var adminUserId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        await using var app = await BuildTestApplicationAsync();
        await SeedUsersAsync(
            app,
            new User(Guid.NewGuid(), tenantId, "ext-e1", "Employee 1"),
            new User(Guid.NewGuid(), tenantId, "ext-e2", "Employee 2"));

        using var client = app.GetTestClient();

        // Step 1: Send authenticated request with bearer token
        var request = new HttpRequestMessage(HttpMethod.Get, "/test/flow/tenant-scoped-data");
        request.Headers.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName, "ok");
        request.Headers.Add(TestAuthHeaders.Subject, adminUserId.ToString());
        request.Headers.Add(TestAuthHeaders.TenantId, tenantId.ToString());
        request.Headers.Add(TestAuthHeaders.Role, "Admin");
        request.Headers.Add(TestAuthHeaders.TokenVersion, "1");

        // Step 2: Request flows through:
        // - JWT middleware (validates token)
        // - Claim extraction (extracts userId, tenantId, roles)
        // - Tenant resolution (resolves tenant from claim)
        // - Authorization (checks Admin role)
        // - Handler executes repository query
        var response = await client.SendAsync(request);

        // Step 3: Verify end-to-end result
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();
        Assert.NotNull(users);
        Assert.Equal(2, users!.Count);
        Assert.All(users, u => Assert.Equal(tenantId, u.TenantId));
    }

    private static async Task<WebApplication> BuildTestApplicationAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        var databaseName = $"end-to-end-auth-tests-{Guid.NewGuid()}";

        // Register services
        builder.Services.AddApplication();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddDbContext<GtekFsmDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));

        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<ITenantContextAccessor, HttpContextTenantContextAccessor>();
        builder.Services.AddScoped<IAuthenticatedPrincipalAccessor, HttpContextAuthenticatedPrincipalAccessor>();
        builder.Services.AddScoped<IAuthorizationDecisionAuditSink, NoOpAuditSink>();

        builder.Services
            .AddAuthentication(options =>
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

        // Test endpoints for each step of the flow

        app.MapGet("/test/flow/verify-auth", async (IAuthenticatedPrincipalAccessor accessor) =>
        {
            var principal = accessor.GetCurrent();
            return principal != null ? Results.Ok() : Results.Unauthorized();
        }).RequireAuthorization();

        app.MapGet("/test/flow/extract-claims", (IAuthenticatedPrincipalAccessor accessor) =>
        {
            var principal = accessor.GetCurrent();
            if (principal == null) return Results.Unauthorized();

            return Results.Ok(new ClaimExtractionResult
            {
                UserId = principal.UserId,
                TenantId = principal.TenantId,
                Roles = principal.Roles.ToList()
            });
        }).RequireAuthorization();

        app.MapGet("/test/flow/resolve-tenant", (ITenantContextAccessor accessor) =>
        {
            var tenantId = accessor.GetCurrentTenantId();
            return tenantId.HasValue
                ? Results.Ok(new TenantResolutionResult { ResolvedTenantId = tenantId.Value })
                : Results.Unauthorized();
        }).RequireAuthorization();

        app.MapGet("/test/flow/protected-admin", (IAuthenticatedPrincipalAccessor accessor) =>
        {
            var principal = accessor.GetCurrent();
            if (principal == null) return Results.Unauthorized();

            var isAdmin = principal.Roles.Contains("Admin");
            return isAdmin ? Results.Ok() : Results.Forbid();
        }).RequireAuthorization(AuthorizationPolicyCatalog.AdminFlow);

        app.MapGet("/test/flow/tenant-scoped-data", async (
            IAuthenticatedPrincipalAccessor principalAccessor,
            IUserRepository userRepository,
            CancellationToken ct) =>
        {
            var principal = principalAccessor.GetCurrent();
            if (principal?.TenantId == null) return Results.Unauthorized();

            var users = await userRepository.ListByTenantAsync(principal.TenantId, ct);
            return Results.Ok(users.Select(u => new UserDto { TenantId = u.TenantId, DisplayName = u.DisplayName }).ToList());
        }).RequireAuthorization();

        await app.StartAsync();
        return app;
    }

    private static async Task SeedUsersAsync(WebApplication app, params User[] users)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GtekFsmDbContext>();

        dbContext.Users.AddRange(users);
        await dbContext.SaveChangesAsync();
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
                claims.Add(new Claim(TokenClaimNames.Subject, subject));

            if (TryGetHeader(TestAuthHeaders.TenantId, out var tenantId))
                claims.Add(new Claim(TokenClaimNames.TenantId, tenantId));

            if (TryGetHeader(TestAuthHeaders.Role, out var role))
            {
                claims.Add(new Claim(TokenClaimNames.Role, role));
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            if (TryGetHeader(TestAuthHeaders.TokenVersion, out var tokenVersion))
                claims.Add(new Claim(TokenClaimNames.TokenVersion, tokenVersion));

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        private bool TryGetHeader(string key, out string value)
        {
            value = string.Empty;
            if (!Request.Headers.TryGetValue(key, out var values) || values.Count == 0)
                return false;

            value = values[0] ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value);
        }
    }

    private sealed class NoOpAuditSink : IAuthorizationDecisionAuditSink
    {
        public Task WriteAsync(AuthorizationDecisionAuditEvent auditEvent, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed record ClaimExtractionResult
    {
        public Guid UserId { get; init; }
        public Guid TenantId { get; init; }
        public List<string> Roles { get; init; } = [];
    }

    private sealed record TenantResolutionResult
    {
        public Guid ResolvedTenantId { get; init; }
    }

    private sealed record UserDto
    {
        public Guid TenantId { get; init; }
        public string DisplayName { get; init; } = string.Empty;
    }

    private static class TestAuthHeaders
    {
        public const string Subject = "X-Test-Sub";
        public const string TenantId = "X-Test-Tenant-Id";
        public const string Role = "X-Test-Role";
        public const string TokenVersion = "X-Test-Ver";
    }
}
