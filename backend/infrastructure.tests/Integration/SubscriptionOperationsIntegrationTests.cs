using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;

using GTEK.FSM.Backend.Api.Authorization;
using GTEK.FSM.Backend.Api.Middleware;
using GTEK.FSM.Backend.Api.Routing;
using GTEK.FSM.Backend.Api.Tenancy;
using GTEK.FSM.Backend.Application;
using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Backend.Application.Persistence.Transactions;
using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Infrastructure.Identity;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Subscriptions.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Subscriptions.Responses;
using GTEK.FSM.Shared.Contracts.Results;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Integration;

public class SubscriptionOperationsIntegrationTests
{
    [Fact]
    public async Task GetOrganizationSubscription_ManagerRole_ReturnsTenantSubscription()
    {
        var tenantId = Guid.NewGuid();
        var subscription = new Subscription(Guid.NewGuid(), tenantId, "PRO", DateTime.UtcNow.AddDays(-30), userLimit: 3);

        var subscriptionStore = new InMemorySubscriptionStore();
        subscriptionStore.Seed(subscription);

        var userStore = new InMemoryUserStore();
        userStore.Seed(new User(Guid.NewGuid(), tenantId, "ext-1", "Alpha"));
        userStore.Seed(new User(Guid.NewGuid(), tenantId, "ext-2", "Beta"));

        var app = await BuildTestApplicationAsync(subscriptionStore, userStore);
        using var client = app.GetTestClient();

        using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/v1/management/subscriptions/organization", "Manager", tenantId, Guid.NewGuid());
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<GetOrganizationSubscriptionResponse>>();
        Assert.NotNull(envelope);
        Assert.True(envelope!.Success);
        Assert.NotNull(envelope.Data);
        Assert.Equal("PRO", envelope.Data!.PlanCode);
        Assert.Equal(3, envelope.Data.UserLimit);
        Assert.Equal(2, envelope.Data.ActiveUsers);
        Assert.Equal(1, envelope.Data.AvailableUserSlots);
    }

    [Fact]
    public async Task PatchOrganizationSubscription_ManagerRole_UpdatesPlanAndLimit()
    {
        var tenantId = Guid.NewGuid();
        var subscription = new Subscription(Guid.NewGuid(), tenantId, "FREE", DateTime.UtcNow.AddDays(-30), userLimit: 2);

        var subscriptionStore = new InMemorySubscriptionStore();
        subscriptionStore.Seed(subscription);

        var userStore = new InMemoryUserStore();
        userStore.Seed(new User(Guid.NewGuid(), tenantId, "ext-1", "Alpha"));

        var app = await BuildTestApplicationAsync(subscriptionStore, userStore);
        using var client = app.GetTestClient();

        using var request = CreateAuthenticatedRequest(HttpMethod.Patch, "/api/v1/management/subscriptions/organization", "Manager", tenantId, Guid.NewGuid());
        request.Content = JsonContent.Create(new UpdateOrganizationSubscriptionRequest
        {
            PlanCode = "ENTERPRISE",
            UserLimit = 10,
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<GetOrganizationSubscriptionResponse>>();
        Assert.NotNull(envelope);
        Assert.True(envelope!.Success);
        Assert.NotNull(envelope.Data);
        Assert.Equal("ENTERPRISE", envelope.Data!.PlanCode);
        Assert.Equal(10, envelope.Data.UserLimit);
        Assert.Equal(9, envelope.Data.AvailableUserSlots);
    }

    [Fact]
    public async Task PatchOrganizationSubscription_UserLimitBelowActiveUsers_ReturnsConflict()
    {
        var tenantId = Guid.NewGuid();
        var subscription = new Subscription(Guid.NewGuid(), tenantId, "PRO", DateTime.UtcNow.AddDays(-30), userLimit: 3);

        var subscriptionStore = new InMemorySubscriptionStore();
        subscriptionStore.Seed(subscription);

        var userStore = new InMemoryUserStore();
        userStore.Seed(new User(Guid.NewGuid(), tenantId, "ext-1", "Alpha"));
        userStore.Seed(new User(Guid.NewGuid(), tenantId, "ext-2", "Beta"));
        userStore.Seed(new User(Guid.NewGuid(), tenantId, "ext-3", "Gamma"));

        var app = await BuildTestApplicationAsync(subscriptionStore, userStore);
        using var client = app.GetTestClient();

        using var request = CreateAuthenticatedRequest(HttpMethod.Patch, "/api/v1/management/subscriptions/organization", "Manager", tenantId, Guid.NewGuid());
        request.Content = JsonContent.Create(new UpdateOrganizationSubscriptionRequest
        {
            PlanCode = "PRO",
            UserLimit = 2,
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("SUBSCRIPTION_USER_LIMIT_CONFLICT", body);
    }

    [Fact]
    public async Task PatchOrganizationSubscription_StaleRowVersion_ReturnsConflict()
    {
        var tenantId = Guid.NewGuid();
        var subscription = new Subscription(Guid.NewGuid(), tenantId, "PRO", DateTime.UtcNow.AddDays(-30), userLimit: 3);

        var subscriptionStore = new InMemorySubscriptionStore();
        subscriptionStore.Seed(subscription);

        var userStore = new InMemoryUserStore();
        userStore.Seed(new User(Guid.NewGuid(), tenantId, "ext-1", "Alpha"));

        var app = await BuildTestApplicationAsync(subscriptionStore, userStore);
        using var client = app.GetTestClient();

        using var request = CreateAuthenticatedRequest(HttpMethod.Patch, "/api/v1/management/subscriptions/organization", "Manager", tenantId, Guid.NewGuid());
        request.Content = JsonContent.Create(new UpdateOrganizationSubscriptionRequest
        {
            PlanCode = "PRO",
            UserLimit = 5,
            RowVersion = Convert.ToBase64String(new byte[] { 1, 2, 3 }),
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("CONCURRENCY_CONFLICT", body);
    }

    [Fact]
    public async Task GetSubscriptionUsers_ManagerRole_ReturnsWithinLimitIndicators()
    {
        var tenantId = Guid.NewGuid();
        var subscription = new Subscription(Guid.NewGuid(), tenantId, "PRO", DateTime.UtcNow.AddDays(-30), userLimit: 2);

        var subscriptionStore = new InMemorySubscriptionStore();
        subscriptionStore.Seed(subscription);

        var userStore = new InMemoryUserStore();
        userStore.Seed(new User(Guid.NewGuid(), tenantId, "ext-1", "Alpha"));
        userStore.Seed(new User(Guid.NewGuid(), tenantId, "ext-2", "Beta"));
        userStore.Seed(new User(Guid.NewGuid(), tenantId, "ext-3", "Gamma"));

        var app = await BuildTestApplicationAsync(subscriptionStore, userStore);
        using var client = app.GetTestClient();

        using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/v1/management/subscriptions/users?page=1&pageSize=10", "Manager", tenantId, Guid.NewGuid());
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<GetSubscriptionUsersListResponse>>();
        Assert.NotNull(envelope);
        Assert.True(envelope!.Success);
        Assert.NotNull(envelope.Data);
        Assert.Equal(3, envelope.Data!.Items.Count);
        Assert.True(envelope.Data.Items[0].IsWithinCurrentPlanLimit);
        Assert.True(envelope.Data.Items[1].IsWithinCurrentPlanLimit);
        Assert.False(envelope.Data.Items[2].IsWithinCurrentPlanLimit);
    }

    [Fact]
    public async Task GetOrganizationSubscription_CustomerRole_ReturnsForbidden()
    {
        var tenantId = Guid.NewGuid();
        var subscription = new Subscription(Guid.NewGuid(), tenantId, "PRO", DateTime.UtcNow.AddDays(-30), userLimit: 3);

        var subscriptionStore = new InMemorySubscriptionStore();
        subscriptionStore.Seed(subscription);
        var userStore = new InMemoryUserStore();

        var app = await BuildTestApplicationAsync(subscriptionStore, userStore);
        using var client = app.GetTestClient();

        using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/v1/management/subscriptions/organization", "Customer", tenantId, Guid.NewGuid());
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string route, string role, Guid tenantId, Guid userId)
    {
        var request = new HttpRequestMessage(method, route);
        request.Headers.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName, "ok");
        request.Headers.Add(TestAuthHeaders.Subject, userId.ToString());
        request.Headers.Add(TestAuthHeaders.TenantId, tenantId.ToString());
        request.Headers.Add(TestAuthHeaders.Role, role);
        request.Headers.Add(TestAuthHeaders.TokenVersion, "1");
        return request;
    }

    private static async Task<WebApplication> BuildTestApplicationAsync(
        InMemorySubscriptionStore subscriptionStore,
        InMemoryUserStore userStore)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddApplication();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IAuthenticatedPrincipalAccessor, HttpContextAuthenticatedPrincipalAccessor>();
        builder.Services.AddScoped<ITenantContextAccessor, HttpContextTenantContextAccessor>();

        builder.Services.AddScoped<ISubscriptionRepository>(_ => subscriptionStore);
        builder.Services.AddScoped<IUserRepository>(_ => userStore);
        builder.Services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();

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

    private sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IUnitOfWorkTransaction>(new NoOpUnitOfWorkTransaction());
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }
    }

    private sealed class NoOpUnitOfWorkTransaction : IUnitOfWorkTransaction
    {
        public Guid? TransactionId => null;

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class InMemorySubscriptionStore : ISubscriptionRepository
    {
        private readonly List<Subscription> items = new();

        public void Seed(Subscription subscription)
        {
            this.items.Add(subscription);
        }

        public Task AddAsync(Subscription aggregate, CancellationToken cancellationToken = default)
        {
            this.items.Add(aggregate);
            return Task.CompletedTask;
        }

        public Task<Subscription?> GetByIdAsync(Guid tenantId, Guid subscriptionId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.items.FirstOrDefault(x => x.TenantId == tenantId && x.Id == subscriptionId));
        }

        public Task<Subscription?> GetActiveByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;
            var item = this.items
                .Where(x => x.TenantId == tenantId && (!x.EndsOnUtc.HasValue || x.EndsOnUtc.Value >= utcNow))
                .OrderByDescending(x => x.StartsOnUtc)
                .FirstOrDefault();

            return Task.FromResult(item);
        }

        public Task<Subscription?> GetActiveForUpdateByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            return this.GetActiveByTenantAsync(tenantId, cancellationToken);
        }

        public Task<IReadOnlyList<Subscription>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Subscription>>(this.items.Where(x => x.TenantId == tenantId).OrderByDescending(x => x.StartsOnUtc).ToList());
        }

        public Task<IReadOnlyList<Subscription>> QueryAsync(SubscriptionQuerySpecification specification, CancellationToken cancellationToken = default)
        {
            var query = this.items.Where(x => x.TenantId == specification.TenantId).AsEnumerable();
            if (specification.ActiveOnly)
            {
                var utcNow = DateTime.UtcNow;
                query = query.Where(x => !x.EndsOnUtc.HasValue || x.EndsOnUtc.Value >= utcNow);
            }

            if (!string.IsNullOrWhiteSpace(specification.PlanCode))
            {
                query = query.Where(x => string.Equals(x.PlanCode, specification.PlanCode, StringComparison.OrdinalIgnoreCase));
            }

            return Task.FromResult<IReadOnlyList<Subscription>>(query.ToList());
        }

        public void Update(Subscription aggregate)
        {
            // No-op for in-memory store.
        }

        public void Remove(Subscription aggregate)
        {
            this.items.Remove(aggregate);
        }
    }

    private sealed class InMemoryUserStore : IUserRepository
    {
        private readonly List<User> items = new();

        public void Seed(User user)
        {
            this.items.Add(user);
        }

        public Task AddAsync(User aggregate, CancellationToken cancellationToken = default)
        {
            this.items.Add(aggregate);
            return Task.CompletedTask;
        }

        public Task<User?> GetByIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.items.FirstOrDefault(x => x.TenantId == tenantId && x.Id == userId));
        }

        public Task<User?> GetByExternalIdentityAsync(Guid tenantId, string externalIdentity, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.items.FirstOrDefault(x => x.TenantId == tenantId && x.ExternalIdentity == externalIdentity));
        }

        public Task<IReadOnlyList<User>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<User>>(this.items.Where(x => x.TenantId == tenantId).OrderBy(x => x.DisplayName).ToList());
        }

        public Task<IReadOnlyList<User>> QueryAsync(UserQuerySpecification specification, CancellationToken cancellationToken = default)
        {
            var query = this.items.Where(x => x.TenantId == specification.TenantId).AsEnumerable();
            if (!string.IsNullOrWhiteSpace(specification.SearchText))
            {
                query = query.Where(x => x.DisplayName.Contains(specification.SearchText, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(specification.ExternalIdentity))
            {
                query = query.Where(x => x.ExternalIdentity == specification.ExternalIdentity);
            }

            return Task.FromResult<IReadOnlyList<User>>(query.ToList());
        }

        public void Update(User aggregate)
        {
            // No-op for in-memory store.
        }

        public void Remove(User aggregate)
        {
            this.items.Remove(aggregate);
        }
    }
}