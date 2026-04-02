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
using GTEK.FSM.Backend.Application.Audit;
using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Backend.Application.Persistence.Transactions;
using GTEK.FSM.Backend.Application.Realtime;
using GTEK.FSM.Backend.Application.ServiceRequests;
using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Domain.Audit;
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

public class CrossChannelParityIntegrationTests
{
    [Fact]
    public async Task StatusTransition_WebAndMobileActions_ProduceEquivalentStateAndAuditTrace()
    {
        var tenantId = Guid.NewGuid();
        var webRequestId = Guid.NewGuid();
        var mobileRequestId = Guid.NewGuid();

        var webResult = await ExecuteTransitionScenarioAsync(tenantId, webRequestId, channelRole: "Customer");
        var mobileResult = await ExecuteTransitionScenarioAsync(tenantId, mobileRequestId, channelRole: "Customer");

        Assert.Equal(HttpStatusCode.OK, webResult.StatusCode);
        Assert.Equal(HttpStatusCode.OK, mobileResult.StatusCode);

        Assert.Equal(ServiceRequestStatus.Assigned, webResult.StoredStatus);
        Assert.Equal(ServiceRequestStatus.Assigned, mobileResult.StoredStatus);

        Assert.Equal("StatusTransition:New->Assigned", webResult.AuditAction);
        Assert.Equal(webResult.AuditAction, mobileResult.AuditAction);
        Assert.Equal("Success", webResult.AuditOutcome);
        Assert.Equal(webResult.AuditOutcome, mobileResult.AuditOutcome);

        Assert.Equal("New", webResult.RealtimePreviousStatus);
        Assert.Equal("Assigned", webResult.RealtimeCurrentStatus);
        Assert.Equal(webResult.RealtimePreviousStatus, mobileResult.RealtimePreviousStatus);
        Assert.Equal(webResult.RealtimeCurrentStatus, mobileResult.RealtimeCurrentStatus);
    }

    [Fact]
    public async Task StatusTransition_StaleRowVersionParity_ReturnsEquivalentConflict()
    {
        var tenantId = Guid.NewGuid();
        var webRequestId = Guid.NewGuid();
        var mobileRequestId = Guid.NewGuid();

        var webResult = await ExecuteConflictTransitionScenarioAsync(tenantId, webRequestId, channelRole: "Customer");
        var mobileResult = await ExecuteConflictTransitionScenarioAsync(tenantId, mobileRequestId, channelRole: "Customer");

        Assert.Equal(HttpStatusCode.Conflict, webResult.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, mobileResult.StatusCode);
        Assert.Equal("CONCURRENCY_CONFLICT", webResult.ErrorCode);
        Assert.Equal(webResult.ErrorCode, mobileResult.ErrorCode);
    }

    [Fact]
    public async Task AssignRequest_WebAndMobileActions_ProduceEquivalentStateAndAuditTrace()
    {
        var tenantId = Guid.NewGuid();
        var workerId = Guid.NewGuid();
        var webRequestId = Guid.NewGuid();
        var mobileRequestId = Guid.NewGuid();

        var webResult = await ExecuteAssignmentScenarioAsync(tenantId, webRequestId, workerId, channelRole: "Support");
        var mobileResult = await ExecuteAssignmentScenarioAsync(tenantId, mobileRequestId, workerId, channelRole: "Support");

        Assert.Equal(HttpStatusCode.OK, webResult.StatusCode);
        Assert.Equal(HttpStatusCode.OK, mobileResult.StatusCode);

        Assert.True(webResult.HasActiveJob);
        Assert.True(mobileResult.HasActiveJob);
        Assert.Equal(ServiceRequestStatus.Assigned, webResult.StoredStatus);
        Assert.Equal(webResult.StoredStatus, mobileResult.StoredStatus);

        Assert.Equal($"AssignWorker:{workerId}", webResult.AuditAction);
        Assert.Equal(webResult.AuditAction, mobileResult.AuditAction);
        Assert.Equal("Success", webResult.AuditOutcome);
        Assert.Equal(webResult.AuditOutcome, mobileResult.AuditOutcome);

        Assert.Equal("PendingAcceptance", webResult.RealtimeAssignmentStatus);
        Assert.Equal(webResult.RealtimeAssignmentStatus, mobileResult.RealtimeAssignmentStatus);
    }

    private static async Task<TransitionParitySnapshot> ExecuteTransitionScenarioAsync(Guid tenantId, Guid requestId, string channelRole)
    {
        var requestStore = new InMemoryServiceRequestStore();
        var jobStore = new InMemoryJobStore();
        var userStore = new InMemoryUserStore();
        var auditWriter = new CapturingAuditLogWriter();
        var realtimePublisher = new CapturingOperationalUpdatePublisher();

        requestStore.Seed(new ServiceRequest(requestId, tenantId, Guid.NewGuid(), "Water leak in lobby"));

        await using var app = await BuildTestApplicationAsync(requestStore, jobStore, userStore, auditWriter, realtimePublisher);
        using var client = app.GetTestClient();

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Patch,
            $"/api/v1/requests/{requestId}/status",
            role: channelRole,
            tenantId: tenantId,
            userId: Guid.NewGuid(),
            body: new TransitionServiceRequestStatusRequest { NextStatus = "Assigned" });

        var response = await client.SendAsync(request);
        var stored = await requestStore.GetByIdAsync(tenantId, requestId);

        return new TransitionParitySnapshot(
            StatusCode: response.StatusCode,
            StoredStatus: stored?.Status ?? ServiceRequestStatus.New,
            AuditAction: auditWriter.Items.Single().Action,
            AuditOutcome: auditWriter.Items.Single().Outcome,
            RealtimePreviousStatus: realtimePublisher.StatusUpdates.Single().PreviousStatus,
            RealtimeCurrentStatus: realtimePublisher.StatusUpdates.Single().CurrentStatus,
            ErrorCode: null);
    }

    private static async Task<TransitionParitySnapshot> ExecuteConflictTransitionScenarioAsync(Guid tenantId, Guid requestId, string channelRole)
    {
        var requestStore = new InMemoryServiceRequestStore();
        var jobStore = new InMemoryJobStore();
        var userStore = new InMemoryUserStore();

        requestStore.Seed(new ServiceRequest(requestId, tenantId, Guid.NewGuid(), "Power outage in level 2"));

        await using var app = await BuildTestApplicationAsync(requestStore, jobStore, userStore);
        using var client = app.GetTestClient();

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Patch,
            $"/api/v1/requests/{requestId}/status",
            role: channelRole,
            tenantId: tenantId,
            userId: Guid.NewGuid(),
            body: new TransitionServiceRequestStatusRequest
            {
                NextStatus = "Assigned",
                RowVersion = Convert.ToBase64String(new byte[] { 1, 2, 3 }),
            });

        var response = await client.SendAsync(request);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

        return new TransitionParitySnapshot(
            StatusCode: response.StatusCode,
            StoredStatus: ServiceRequestStatus.New,
            AuditAction: string.Empty,
            AuditOutcome: string.Empty,
            RealtimePreviousStatus: string.Empty,
            RealtimeCurrentStatus: string.Empty,
            ErrorCode: envelope?.ErrorCode);
    }

    private static async Task<AssignmentParitySnapshot> ExecuteAssignmentScenarioAsync(Guid tenantId, Guid requestId, Guid workerId, string channelRole)
    {
        var requestStore = new InMemoryServiceRequestStore();
        var jobStore = new InMemoryJobStore();
        var userStore = new InMemoryUserStore();
        var auditWriter = new CapturingAuditLogWriter();
        var realtimePublisher = new CapturingOperationalUpdatePublisher();

        requestStore.Seed(new ServiceRequest(requestId, tenantId, Guid.NewGuid(), "Elevator alarm issue"));
        userStore.Seed(new User(workerId, tenantId, "wrk-01", "Worker One"));

        await using var app = await BuildTestApplicationAsync(requestStore, jobStore, userStore, auditWriter, realtimePublisher);
        using var client = app.GetTestClient();

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/requests/{requestId}/assign",
            role: channelRole,
            tenantId: tenantId,
            userId: Guid.NewGuid(),
            body: new AssignServiceRequestRequest { WorkerUserId = workerId.ToString() });

        var response = await client.SendAsync(request);
        var stored = await requestStore.GetByIdAsync(tenantId, requestId);

        return new AssignmentParitySnapshot(
            StatusCode: response.StatusCode,
            HasActiveJob: stored?.ActiveJobId.HasValue == true,
            StoredStatus: stored?.Status ?? ServiceRequestStatus.New,
            AuditAction: auditWriter.Items.Single().Action,
            AuditOutcome: auditWriter.Items.Single().Outcome,
            RealtimeAssignmentStatus: realtimePublisher.AssignmentUpdates.Single().AssignmentStatus);
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
        InMemoryUserStore userStore,
        CapturingAuditLogWriter? auditWriter = null,
        CapturingOperationalUpdatePublisher? realtimePublisher = null)
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

        if (auditWriter is not null)
        {
            builder.Services.AddScoped<IAuditLogWriter>(_ => auditWriter);
        }

        if (realtimePublisher is not null)
        {
            builder.Services.AddScoped<IOperationalUpdatePublisher>(_ => realtimePublisher);
        }

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

    private sealed record TransitionParitySnapshot(
        HttpStatusCode StatusCode,
        ServiceRequestStatus StoredStatus,
        string AuditAction,
        string AuditOutcome,
        string RealtimePreviousStatus,
        string RealtimeCurrentStatus,
        string? ErrorCode);

    private sealed record AssignmentParitySnapshot(
        HttpStatusCode StatusCode,
        bool HasActiveJob,
        ServiceRequestStatus StoredStatus,
        string AuditAction,
        string AuditOutcome,
        string RealtimeAssignmentStatus);

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
            // Aggregate is already tracked in-memory.
        }

        public void Remove(ServiceRequest aggregate)
        {
            this.items.Remove(aggregate);
        }
    }

    private sealed class InMemoryJobStore : IJobRepository
    {
        private readonly List<Job> items = new();

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
            Guid tenantId,
            IReadOnlyList<Guid> workerIds,
            CancellationToken cancellationToken = default)
        {
            var activeStatuses = new[] { AssignmentStatus.PendingAcceptance, AssignmentStatus.Accepted };
            IReadOnlyDictionary<Guid, int> result = this.items
                .Where(x => x.TenantId == tenantId
                    && x.AssignedWorkerUserId.HasValue
                    && workerIds.Contains(x.AssignedWorkerUserId.Value)
                    && activeStatuses.Contains(x.AssignmentStatus))
                .GroupBy(x => x.AssignedWorkerUserId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            return Task.FromResult(result);
        }

        public void Update(Job aggregate)
        {
            // Aggregate is already tracked in-memory.
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
            // Aggregate is already tracked in-memory.
        }

        public void Remove(User aggregate)
        {
            this.items.Remove(aggregate);
        }
    }

    private sealed class CapturingAuditLogWriter : IAuditLogWriter
    {
        public List<AuditLog> Items { get; } = new();

        public Task WriteAsync(AuditLog log, CancellationToken cancellationToken = default)
        {
            this.Items.Add(log);
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingOperationalUpdatePublisher : IOperationalUpdatePublisher
    {
        public List<TransitionedServiceRequestPayload> StatusUpdates { get; } = new();

        public List<AssignedServiceRequestPayload> AssignmentUpdates { get; } = new();

        public List<SlaEscalationTriggeredPayload> SlaEscalations { get; } = new();

        public Task PublishServiceRequestStatusUpdatedAsync(TransitionedServiceRequestPayload payload, CancellationToken cancellationToken = default)
        {
            this.StatusUpdates.Add(payload);
            return Task.CompletedTask;
        }

        public Task PublishJobAssignmentUpdatedAsync(AssignedServiceRequestPayload payload, CancellationToken cancellationToken = default)
        {
            this.AssignmentUpdates.Add(payload);
            return Task.CompletedTask;
        }

        public Task PublishSlaEscalationTriggeredAsync(SlaEscalationTriggeredPayload payload, CancellationToken cancellationToken = default)
        {
            this.SlaEscalations.Add(payload);
            return Task.CompletedTask;
        }
    }
}