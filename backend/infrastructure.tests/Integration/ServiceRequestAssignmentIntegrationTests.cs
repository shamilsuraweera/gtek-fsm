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
using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Backend.Infrastructure.Identity;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Responses;
using GTEK.FSM.Shared.Contracts.Results;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Integration;

public class ServiceRequestAssignmentIntegrationTests
{
    [Fact]
    public async Task AssignRequest_SupportRole_SucceedsAndCreatesJobLink()
    {
        var tenantId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var workerId = Guid.NewGuid();

        var requestStore = new InMemoryServiceRequestStore();
        var jobStore = new InMemoryJobStore();
        var userStore = new InMemoryUserStore();

        requestStore.Seed(new ServiceRequest(requestId, tenantId, Guid.NewGuid(), "Elevator alarm issue"));
        userStore.Seed(new User(workerId, tenantId, "wrk-01", "Worker One"));

        await using var app = await BuildTestApplicationAsync(requestStore, jobStore, userStore);
        using var client = app.GetTestClient();

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/requests/{requestId}/assign",
            role: "Support",
            tenantId: tenantId,
            userId: Guid.NewGuid(),
            body: new AssignServiceRequestRequest { WorkerUserId = workerId.ToString() });

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<ServiceRequestAssignmentResponse>>();
        Assert.NotNull(envelope);
        Assert.True(envelope!.Success);
        Assert.NotNull(envelope.Data);
        Assert.Equal(workerId.ToString(), envelope.Data!.CurrentWorkerUserId);

        Assert.Single(jobStore.Items);
        Assert.NotNull(requestStore.Items[0].ActiveJobId);
    }

    [Fact]
    public async Task ReassignRequest_ManagerRole_SucceedsAndPreservesPreviousWorkerContext()
    {
        var tenantId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var firstWorker = Guid.NewGuid();
        var secondWorker = Guid.NewGuid();

        var requestStore = new InMemoryServiceRequestStore();
        var jobStore = new InMemoryJobStore();
        var userStore = new InMemoryUserStore();

        var seededRequest = new ServiceRequest(requestId, tenantId, Guid.NewGuid(), "Server rack overheating");
        seededRequest.TransitionTo(ServiceRequestStatus.Assigned);

        var seededJob = new Job(Guid.NewGuid(), tenantId, requestId);
        seededJob.AssignWorker(firstWorker);
        seededRequest.LinkJob(seededJob.Id);

        requestStore.Seed(seededRequest);
        jobStore.Seed(seededJob);
        userStore.Seed(new User(firstWorker, tenantId, "wrk-a", "Worker A"));
        userStore.Seed(new User(secondWorker, tenantId, "wrk-b", "Worker B"));

        await using var app = await BuildTestApplicationAsync(requestStore, jobStore, userStore);
        using var client = app.GetTestClient();

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/requests/{requestId}/reassign",
            role: "Manager",
            tenantId: tenantId,
            userId: Guid.NewGuid(),
            body: new ReassignServiceRequestRequest { WorkerUserId = secondWorker.ToString() });

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<ServiceRequestAssignmentResponse>>();
        Assert.NotNull(envelope);
        Assert.True(envelope!.Success);
        Assert.NotNull(envelope.Data);
        Assert.Equal(firstWorker.ToString(), envelope.Data!.PreviousWorkerUserId);
        Assert.Equal(secondWorker.ToString(), envelope.Data.CurrentWorkerUserId);

        Assert.Single(jobStore.Items);
        Assert.Equal(secondWorker, jobStore.Items[0].AssignedWorkerUserId);
    }

    [Fact]
    public async Task AssignRequest_CustomerRole_ReturnsForbidden()
    {
        var tenantId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var workerId = Guid.NewGuid();

        var requestStore = new InMemoryServiceRequestStore();
        var jobStore = new InMemoryJobStore();
        var userStore = new InMemoryUserStore();

        requestStore.Seed(new ServiceRequest(requestId, tenantId, Guid.NewGuid(), "Door lock not working"));
        userStore.Seed(new User(workerId, tenantId, "wrk-02", "Worker Two"));

        await using var app = await BuildTestApplicationAsync(requestStore, jobStore, userStore);
        using var client = app.GetTestClient();

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/requests/{requestId}/assign",
            role: "Customer",
            tenantId: tenantId,
            userId: Guid.NewGuid(),
            body: new AssignServiceRequestRequest { WorkerUserId = workerId.ToString() });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("AUTH_FORBIDDEN_ROLE", body);
    }

    [Fact]
    public async Task ReassignRequest_WorkerNotInTenant_ReturnsNotFound()
    {
        var tenantId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var firstWorker = Guid.NewGuid();

        var requestStore = new InMemoryServiceRequestStore();
        var jobStore = new InMemoryJobStore();
        var userStore = new InMemoryUserStore();

        var seededRequest = new ServiceRequest(requestId, tenantId, Guid.NewGuid(), "HVAC sensor fault");
        seededRequest.TransitionTo(ServiceRequestStatus.Assigned);

        var seededJob = new Job(Guid.NewGuid(), tenantId, requestId);
        seededJob.AssignWorker(firstWorker);
        seededRequest.LinkJob(seededJob.Id);

        requestStore.Seed(seededRequest);
        jobStore.Seed(seededJob);
        userStore.Seed(new User(firstWorker, tenantId, "wrk-c", "Worker C"));

        await using var app = await BuildTestApplicationAsync(requestStore, jobStore, userStore);
        using var client = app.GetTestClient();

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/requests/{requestId}/reassign",
            role: "Support",
            tenantId: tenantId,
            userId: Guid.NewGuid(),
            body: new ReassignServiceRequestRequest { WorkerUserId = Guid.NewGuid().ToString() });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("WORKER_NOT_FOUND_IN_TENANT", body);
    }

    [Fact]
    public async Task AssignRequest_StaleRowVersion_ReturnsConflict()
    {
        var tenantId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var workerId = Guid.NewGuid();

        var requestStore = new InMemoryServiceRequestStore();
        var jobStore = new InMemoryJobStore();
        var userStore = new InMemoryUserStore();

        requestStore.Seed(new ServiceRequest(requestId, tenantId, Guid.NewGuid(), "Elevator alarm issue"));
        userStore.Seed(new User(workerId, tenantId, "wrk-01", "Worker One"));

        await using var app = await BuildTestApplicationAsync(requestStore, jobStore, userStore);
        using var client = app.GetTestClient();

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/requests/{requestId}/assign",
            role: "Support",
            tenantId: tenantId,
            userId: Guid.NewGuid(),
            body: new AssignServiceRequestRequest
            {
                WorkerUserId = workerId.ToString(),
                RowVersion = Convert.ToBase64String(new byte[] { 1, 2, 3 }),
            });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("CONCURRENCY_CONFLICT", body);
    }

    private static HttpRequestMessage CreateAuthenticatedRequest(
        HttpMethod method,
        string route,
        string role,
        Guid tenantId,
        Guid userId,
        object body)
    {
        var request = new HttpRequestMessage(method, route);
        request.Headers.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName, "ok");
        request.Headers.Add(TestAuthHeaders.Subject, userId.ToString());
        request.Headers.Add(TestAuthHeaders.TenantId, tenantId.ToString());
        request.Headers.Add(TestAuthHeaders.Role, role);
        request.Headers.Add(TestAuthHeaders.TokenVersion, "1");
        request.Content = JsonContent.Create(body);
        return request;
    }

    private static async Task<WebApplication> BuildTestApplicationAsync(
        InMemoryServiceRequestStore requestStore,
        InMemoryJobStore jobStore,
        InMemoryUserStore userStore)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddApplication();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IAuthenticatedPrincipalAccessor, HttpContextAuthenticatedPrincipalAccessor>();
        builder.Services.AddScoped<ITenantContextAccessor, HttpContextTenantContextAccessor>();

        builder.Services.AddScoped<IServiceRequestRepository>(_ => requestStore);
        builder.Services.AddScoped<IJobRepository>(_ => jobStore);
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
            return Task.FromResult(1);
        }
    }

    private sealed class NoOpUnitOfWorkTransaction : IUnitOfWorkTransaction
    {
        public Guid? TransactionId => Guid.NewGuid();

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

    private sealed class InMemoryServiceRequestStore : IServiceRequestRepository
    {
        private readonly List<ServiceRequest> items = new();

        public IReadOnlyList<ServiceRequest> Items => this.items;

        public void Seed(ServiceRequest request)
        {
            this.items.Add(request);
        }

        public Task AddAsync(ServiceRequest aggregate, CancellationToken cancellationToken = default)
        {
            this.items.Add(aggregate);
            return Task.CompletedTask;
        }

        public Task<ServiceRequest?> GetByIdAsync(Guid tenantId, Guid requestId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.items.FirstOrDefault(x => x.TenantId == tenantId && x.Id == requestId));
        }

        public Task<ServiceRequest?> GetForUpdateAsync(Guid tenantId, Guid requestId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.items.FirstOrDefault(x => x.TenantId == tenantId && x.Id == requestId));
        }

        public Task<IReadOnlyList<ServiceRequest>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ServiceRequest> result = this.items.Where(x => x.TenantId == tenantId).ToList();
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<ServiceRequest>> ListByCustomerAsync(Guid tenantId, Guid customerUserId, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ServiceRequest> result = this.items.Where(x => x.TenantId == tenantId && x.CustomerUserId == customerUserId).ToList();
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<ServiceRequest>> QueryAsync(ServiceRequestQuerySpecification specification, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ServiceRequest> result = this.items.Where(x => x.TenantId == specification.TenantId).ToList();
            return Task.FromResult(result);
        }

        public Task<int> CountAsync(ServiceRequestQuerySpecification specification, CancellationToken cancellationToken = default)
        {
            var count = this.items.Count(x => x.TenantId == specification.TenantId);
            return Task.FromResult(count);
        }

        public void Update(ServiceRequest aggregate)
        {
            // No-op for in-memory store.
        }

        public void Remove(ServiceRequest aggregate)
        {
            this.items.Remove(aggregate);
        }
    }

    private sealed class InMemoryJobStore : IJobRepository
    {
        private readonly List<Job> items = new();

        public IReadOnlyList<Job> Items => this.items;

        public void Seed(Job job)
        {
            this.items.Add(job);
        }

        public Task AddAsync(Job aggregate, CancellationToken cancellationToken = default)
        {
            this.items.Add(aggregate);
            return Task.CompletedTask;
        }

        public Task<Job?> GetByIdAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.items.FirstOrDefault(x => x.TenantId == tenantId && x.Id == jobId));
        }

        public Task<Job?> GetForUpdateAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.items.FirstOrDefault(x => x.TenantId == tenantId && x.Id == jobId));
        }

        public Task<IReadOnlyList<Job>> ListByServiceRequestAsync(Guid tenantId, Guid serviceRequestId, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Job> result = this.items.Where(x => x.TenantId == tenantId && x.ServiceRequestId == serviceRequestId).ToList();
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<Job>> ListByWorkerAsync(Guid tenantId, Guid workerUserId, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Job> result = this.items.Where(x => x.TenantId == tenantId && x.AssignedWorkerUserId == workerUserId).ToList();
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<Job>> QueryAsync(JobQuerySpecification specification, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Job> result = this.items.Where(x => x.TenantId == specification.TenantId).ToList();
            return Task.FromResult(result);
        }

        public Task<int> CountAsync(JobQuerySpecification specification, CancellationToken cancellationToken = default)
        {
            var count = this.items.Count(x => x.TenantId == specification.TenantId);
            return Task.FromResult(count);
        }

        public Task<IReadOnlyDictionary<Guid, int>> GetActiveJobCountsByWorkerAsync(
            Guid tenantId, IReadOnlyList<Guid> workerIds, CancellationToken cancellationToken = default)
        {
            var activeStatuses = new[] { AssignmentStatus.PendingAcceptance, AssignmentStatus.Accepted };
            IReadOnlyDictionary<Guid, int> result = this.items
                .Where(x => x.TenantId == tenantId && x.AssignedWorkerUserId.HasValue
                    && workerIds.Contains(x.AssignedWorkerUserId!.Value)
                    && activeStatuses.Contains(x.AssignmentStatus))
                .GroupBy(x => x.AssignedWorkerUserId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());
            return Task.FromResult(result);
        }

        public void Update(Job aggregate)
        {
            // No-op for in-memory store.
        }

        public void Remove(Job aggregate)
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
            IReadOnlyList<User> result = this.items.Where(x => x.TenantId == tenantId).ToList();
            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<User>> QueryAsync(UserQuerySpecification specification, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<User> result = this.items.Where(x => x.TenantId == specification.TenantId).ToList();
            return Task.FromResult(result);
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
