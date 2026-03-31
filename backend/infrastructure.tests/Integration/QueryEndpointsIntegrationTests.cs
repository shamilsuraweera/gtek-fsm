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
using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Backend.Infrastructure.Identity;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Jobs.Responses;
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

public class QueryEndpointsIntegrationTests
{
    [Fact]
    public async Task GetRequests_CustomerScope_ReturnsOnlyCustomerItems()
    {
        var tenantId = Guid.NewGuid();
        var customerA = Guid.NewGuid();
        var customerB = Guid.NewGuid();

        var requestStore = new InMemoryServiceRequestStore();
        requestStore.Seed(new ServiceRequest(Guid.NewGuid(), tenantId, customerA, "Request-A"));
        requestStore.Seed(new ServiceRequest(Guid.NewGuid(), tenantId, customerB, "Request-B"));

        var jobStore = new InMemoryJobStore();

        var app = await BuildTestApplicationAsync(requestStore, jobStore);
        using var client = app.GetTestClient();

        using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/v1/requests?page=1&pageSize=50", "Customer", tenantId, customerA);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<GetRequestsListResponse>>();
        Assert.NotNull(envelope);
        Assert.True(envelope!.Success);
        Assert.NotNull(envelope.Data);
        Assert.Single(envelope.Data!.Items);
        Assert.Equal("Request-A", envelope.Data.Items[0].Summary);
        Assert.Equal(1, envelope.Data.Pagination.Total);
    }

    [Fact]
    public async Task GetJobs_WorkerScope_ReturnsOnlyAssignedItems()
    {
        var tenantId = Guid.NewGuid();
        var workerA = Guid.NewGuid();
        var workerB = Guid.NewGuid();

        var requestStore = new InMemoryServiceRequestStore();
        var requestA = new ServiceRequest(Guid.NewGuid(), tenantId, Guid.NewGuid(), "Request-A");
        var requestB = new ServiceRequest(Guid.NewGuid(), tenantId, Guid.NewGuid(), "Request-B");
        requestStore.Seed(requestA);
        requestStore.Seed(requestB);

        var jobStore = new InMemoryJobStore();
        var jobA = new Job(Guid.NewGuid(), tenantId, requestA.Id);
        jobA.AssignWorker(workerA);
        var jobB = new Job(Guid.NewGuid(), tenantId, requestB.Id);
        jobB.AssignWorker(workerB);
        jobStore.Seed(jobA);
        jobStore.Seed(jobB);

        var app = await BuildTestApplicationAsync(requestStore, jobStore);
        using var client = app.GetTestClient();

        using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/v1/jobs?page=1&pageSize=50", "Worker", tenantId, workerA);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<GetJobsListResponse>>();
        Assert.NotNull(envelope);
        Assert.True(envelope!.Success);
        Assert.NotNull(envelope.Data);
        Assert.Single(envelope.Data!.Items);
        Assert.Equal(workerA.ToString(), envelope.Data.Items[0].AssignedTo);
        Assert.Equal(1, envelope.Data.Pagination.Total);
    }

    [Fact]
    public async Task GetJobs_CustomerRole_ReturnsForbidden()
    {
        var tenantId = Guid.NewGuid();

        var requestStore = new InMemoryServiceRequestStore();
        var jobStore = new InMemoryJobStore();

        var app = await BuildTestApplicationAsync(requestStore, jobStore);
        using var client = app.GetTestClient();

        using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/v1/jobs", "Customer", tenantId, Guid.NewGuid());
        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("AUTH_FORBIDDEN_ROLE", body);
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
        InMemoryServiceRequestStore requestStore,
        InMemoryJobStore jobStore)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddApplication();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IAuthenticatedPrincipalAccessor, HttpContextAuthenticatedPrincipalAccessor>();
        builder.Services.AddScoped<ITenantContextAccessor, HttpContextTenantContextAccessor>();

        builder.Services.AddScoped<IServiceRequestRepository>(_ => requestStore);
        builder.Services.AddScoped<IJobRepository>(_ => jobStore);

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

    private sealed class InMemoryServiceRequestStore : IServiceRequestRepository
    {
        private readonly List<ServiceRequest> items = new();

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
            return Task.FromResult<IReadOnlyList<ServiceRequest>>(this.items.Where(x => x.TenantId == tenantId).ToList());
        }

        public Task<IReadOnlyList<ServiceRequest>> ListByCustomerAsync(Guid tenantId, Guid customerUserId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ServiceRequest>>(this.items.Where(x => x.TenantId == tenantId && x.CustomerUserId == customerUserId).ToList());
        }

        public Task<IReadOnlyList<ServiceRequest>> QueryAsync(ServiceRequestQuerySpecification specification, CancellationToken cancellationToken = default)
        {
            var query = this.items.Where(x => x.TenantId == specification.TenantId).AsEnumerable();

            if (specification.CustomerUserId.HasValue)
            {
                query = query.Where(x => x.CustomerUserId == specification.CustomerUserId.Value);
            }

            if (specification.Status.HasValue)
            {
                query = query.Where(x => x.Status == specification.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(specification.SearchText))
            {
                query = query.Where(x => x.Title.Contains(specification.SearchText, StringComparison.OrdinalIgnoreCase));
            }

            var sorted = specification.SortBy switch
            {
                ServiceRequestSortField.Status when specification.SortDirection == SortDirection.Ascending => query.OrderBy(x => x.Status),
                ServiceRequestSortField.Status => query.OrderByDescending(x => x.Status),
                ServiceRequestSortField.Title when specification.SortDirection == SortDirection.Ascending => query.OrderBy(x => x.Title),
                ServiceRequestSortField.Title => query.OrderByDescending(x => x.Title),
                ServiceRequestSortField.CreatedAtUtc when specification.SortDirection == SortDirection.Ascending => query.OrderBy(x => x.CreatedAtUtc),
                _ => query.OrderByDescending(x => x.CreatedAtUtc),
            };

            var page = specification.Page ?? new PageSpecification();
            return Task.FromResult<IReadOnlyList<ServiceRequest>>(sorted.Skip(page.Skip).Take(page.Take).ToList());
        }

        public Task<int> CountAsync(ServiceRequestQuerySpecification specification, CancellationToken cancellationToken = default)
        {
            var query = this.items.Where(x => x.TenantId == specification.TenantId).AsEnumerable();

            if (specification.CustomerUserId.HasValue)
            {
                query = query.Where(x => x.CustomerUserId == specification.CustomerUserId.Value);
            }

            if (specification.Status.HasValue)
            {
                query = query.Where(x => x.Status == specification.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(specification.SearchText))
            {
                query = query.Where(x => x.Title.Contains(specification.SearchText, StringComparison.OrdinalIgnoreCase));
            }

            return Task.FromResult(query.Count());
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
            return Task.FromResult<IReadOnlyList<Job>>(this.items.Where(x => x.TenantId == tenantId && x.ServiceRequestId == serviceRequestId).ToList());
        }

        public Task<IReadOnlyList<Job>> ListByWorkerAsync(Guid tenantId, Guid workerUserId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Job>>(this.items.Where(x => x.TenantId == tenantId && x.AssignedWorkerUserId == workerUserId).ToList());
        }

        public Task<IReadOnlyList<Job>> QueryAsync(JobQuerySpecification specification, CancellationToken cancellationToken = default)
        {
            var query = this.items.Where(x => x.TenantId == specification.TenantId).AsEnumerable();

            if (specification.AssignedWorkerUserId.HasValue)
            {
                query = query.Where(x => x.AssignedWorkerUserId == specification.AssignedWorkerUserId.Value);
            }

            if (specification.AssignmentStatus.HasValue)
            {
                query = query.Where(x => x.AssignmentStatus == specification.AssignmentStatus.Value);
            }

            if (specification.ServiceRequestId.HasValue)
            {
                query = query.Where(x => x.ServiceRequestId == specification.ServiceRequestId.Value);
            }

            var sorted = specification.SortBy switch
            {
                JobSortField.AssignmentStatus when specification.SortDirection == SortDirection.Ascending => query.OrderBy(x => x.AssignmentStatus),
                JobSortField.AssignmentStatus => query.OrderByDescending(x => x.AssignmentStatus),
                JobSortField.CreatedAtUtc when specification.SortDirection == SortDirection.Ascending => query.OrderBy(x => x.CreatedAtUtc),
                _ => query.OrderByDescending(x => x.CreatedAtUtc),
            };

            var page = specification.Page ?? new PageSpecification();
            return Task.FromResult<IReadOnlyList<Job>>(sorted.Skip(page.Skip).Take(page.Take).ToList());
        }

        public Task<int> CountAsync(JobQuerySpecification specification, CancellationToken cancellationToken = default)
        {
            var query = this.items.Where(x => x.TenantId == specification.TenantId).AsEnumerable();

            if (specification.AssignedWorkerUserId.HasValue)
            {
                query = query.Where(x => x.AssignedWorkerUserId == specification.AssignedWorkerUserId.Value);
            }

            if (specification.AssignmentStatus.HasValue)
            {
                query = query.Where(x => x.AssignmentStatus == specification.AssignmentStatus.Value);
            }

            if (specification.ServiceRequestId.HasValue)
            {
                query = query.Where(x => x.ServiceRequestId == specification.ServiceRequestId.Value);
            }

            return Task.FromResult(query.Count());
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
}
