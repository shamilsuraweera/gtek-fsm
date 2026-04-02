using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;

using GTEK.FSM.Backend.Api.Authorization;
using GTEK.FSM.Backend.Api.Middleware;
using GTEK.FSM.Backend.Api.Tenancy;
using GTEK.FSM.Backend.Application;
using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Backend.Infrastructure.Identity;
using GTEK.FSM.Backend.Infrastructure.Persistence;
using GTEK.FSM.Backend.Infrastructure.Persistence.Repositories;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Integration;

public class AuthenticatedTenantQueryPathIntegrationTests
{
    [Fact]
    public async Task AuthenticatedListQuery_UsesPrincipalTenantAndBlocksCrossTenantLeakage()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await using var app = await BuildTestApplicationAsync();
        await SeedUsersAsync(
            app,
            new User(Guid.NewGuid(), tenantA, "ext-a-1", "Alpha A1"),
            new User(Guid.NewGuid(), tenantA, "ext-a-2", "Alpha A2"),
            new User(Guid.NewGuid(), tenantB, "ext-b-1", "Beta B1"));

        using var client = app.GetTestClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/test/query/users/list");
        request.Headers.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName, "ok");
        request.Headers.Add(TestAuthHeaders.Subject, Guid.NewGuid().ToString());
        request.Headers.Add(TestAuthHeaders.TenantId, tenantA.ToString());
        request.Headers.Add(TestAuthHeaders.Role, "Support");
        request.Headers.Add(TestAuthHeaders.TokenVersion, "1");

        var response = await client.SendAsync(request);
        var payload = await response.Content.ReadFromJsonAsync<List<TenantScopedUserDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal(2, payload!.Count);
        Assert.All(payload, user => Assert.Equal(tenantA, user.TenantId));
    }

    [Fact]
    public async Task AuthenticatedSpecificationQuery_RemainsTenantScopedUnderSearchFilter()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await using var app = await BuildTestApplicationAsync();
        await SeedUsersAsync(
            app,
            new User(Guid.NewGuid(), tenantA, "shared-a", "Shared Person"),
            new User(Guid.NewGuid(), tenantB, "shared-b", "Shared Person"),
            new User(Guid.NewGuid(), tenantA, "other-a", "Tenant A Only"));

        using var client = app.GetTestClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/test/query/users/search?term=Shared");
        request.Headers.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName, "ok");
        request.Headers.Add(TestAuthHeaders.Subject, Guid.NewGuid().ToString());
        request.Headers.Add(TestAuthHeaders.TenantId, tenantA.ToString());
        request.Headers.Add(TestAuthHeaders.Role, "Support");
        request.Headers.Add(TestAuthHeaders.TokenVersion, "1");

        var response = await client.SendAsync(request);
        var payload = await response.Content.ReadFromJsonAsync<List<TenantScopedUserDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Single(payload!);
        Assert.Equal(tenantA, payload[0].TenantId);
        Assert.Equal("Shared Person", payload[0].DisplayName);
    }

    private static async Task<WebApplication> BuildTestApplicationAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var databaseName = $"gtek-fsm-auth-query-path-tests-{Guid.NewGuid()}";

        builder.Services.AddApplication();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IAuthenticatedPrincipalAccessor, HttpContextAuthenticatedPrincipalAccessor>();
        builder.Services.AddScoped<ITenantContextAccessor, HttpContextTenantContextAccessor>();
        builder.Services.AddScoped<IUserRepository, UserRepository>();

        builder.Services.AddDbContext<GtekFsmDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));

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

        app.MapGet("/test/query/users/list", async (
                IAuthenticatedPrincipalAccessor principalAccessor,
                IUserRepository userRepository,
                CancellationToken cancellationToken) =>
            {
                var principal = principalAccessor.GetCurrent();
                if (principal is null)
                {
                    return Results.Unauthorized();
                }

                var users = await userRepository.ListByTenantAsync(principal.TenantId, cancellationToken);
                return Results.Ok(users.Select(ToDto).ToList());
            })
            .RequireAuthorization(AuthorizationPolicyCatalog.SystemPing);

        app.MapGet("/test/query/users/search", async (
                string term,
                IAuthenticatedPrincipalAccessor principalAccessor,
                IUserRepository userRepository,
                CancellationToken cancellationToken) =>
            {
                var principal = principalAccessor.GetCurrent();
                if (principal is null)
                {
                    return Results.Unauthorized();
                }

                var users = await userRepository.QueryAsync(
                    new UserQuerySpecification(
                        TenantId: principal.TenantId,
                        SearchText: term,
                        Page: new PageSpecification(1, 50),
                        SortBy: UserSortField.DisplayName,
                        SortDirection: SortDirection.Ascending),
                    cancellationToken);

                return Results.Ok(users.Select(ToDto).ToList());
            })
            .RequireAuthorization(AuthorizationPolicyCatalog.SystemPing);

        await app.StartAsync();
        return app;
    }

    private static async Task SeedUsersAsync(WebApplication app, params User[] users)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GtekFsmDbContext>();

        await dbContext.Users.AddRangeAsync(users);
        await dbContext.SaveChangesAsync();
    }

    private static TenantScopedUserDto ToDto(User user)
    {
        return new TenantScopedUserDto(user.Id, user.TenantId, user.DisplayName);
    }

    private sealed record TenantScopedUserDto(Guid Id, Guid TenantId, string DisplayName);

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
