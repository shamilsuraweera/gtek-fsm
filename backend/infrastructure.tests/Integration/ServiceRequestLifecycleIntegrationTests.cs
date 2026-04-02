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

public class ServiceRequestLifecycleIntegrationTests
{
    [Fact]
    public async Task TransitionStatus_LegalTransition_ReturnsOkAndUpdatesStatus()
    {
        var tenantId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var store = new InMemoryServiceRequestStore();
        store.Seed(new ServiceRequest(requestId, tenantId, Guid.NewGuid(), "Water leak in lobby"));

        await using var app = await BuildTestApplicationAsync(store);
        using var client = app.GetTestClient();

        using var request = CreateAuthenticatedRequest(
            method: HttpMethod.Patch,
            route: $"/api/v1/requests/{requestId}/status",
            role: "Customer",
            tenantId: tenantId,
            userId: Guid.NewGuid(),
            body: new TransitionServiceRequestStatusRequest { NextStatus = "Assigned" });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<TransitionServiceRequestStatusResponse>>();
        Assert.NotNull(envelope);
        Assert.True(envelope!.Success);
        Assert.NotNull(envelope.Data);
        Assert.Equal("New", envelope.Data!.PreviousStatus);
        Assert.Equal("Assigned", envelope.Data.CurrentStatus);

        var stored = await store.GetByIdAsync(tenantId, requestId);
        Assert.NotNull(stored);
        Assert.Equal(ServiceRequestStatus.Assigned, stored!.Status);
    }

    [Fact]
    public async Task TransitionStatus_IllegalTransition_ReturnsBadRequest()
    {
        var tenantId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var store = new InMemoryServiceRequestStore();
        store.Seed(new ServiceRequest(requestId, tenantId, Guid.NewGuid(), "Power outage in level 2"));

        await using var app = await BuildTestApplicationAsync(store);
        using var client = app.GetTestClient();

        using var request = CreateAuthenticatedRequest(
            method: HttpMethod.Patch,
            route: $"/api/v1/requests/{requestId}/status",
            role: "Customer",
            tenantId: tenantId,
            userId: Guid.NewGuid(),
            body: new TransitionServiceRequestStatusRequest { NextStatus = "Completed" });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("REQUEST_TRANSITION_INVALID", body);

        var stored = await store.GetByIdAsync(tenantId, requestId);
        Assert.NotNull(stored);
        Assert.Equal(ServiceRequestStatus.New, stored!.Status);
    }

    [Fact]
    public async Task TransitionStatus_CrossTenantLookup_ReturnsNotFound()
    {
        var requestTenant = Guid.NewGuid();
        var callerTenant = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var store = new InMemoryServiceRequestStore();
        store.Seed(new ServiceRequest(requestId, requestTenant, Guid.NewGuid(), "HVAC maintenance"));

        await using var app = await BuildTestApplicationAsync(store);
        using var client = app.GetTestClient();

        using var request = CreateAuthenticatedRequest(
            method: HttpMethod.Patch,
            route: $"/api/v1/requests/{requestId}/status",
            role: "Customer",
            tenantId: callerTenant,
            userId: Guid.NewGuid(),
            body: new TransitionServiceRequestStatusRequest { NextStatus = "Assigned" });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("REQUEST_NOT_FOUND", body);

        var stored = await store.GetByIdAsync(requestTenant, requestId);
        Assert.NotNull(stored);
        Assert.Equal(ServiceRequestStatus.New, stored!.Status);
    }

    [Fact]
    public async Task TransitionStatus_StaleRowVersion_ReturnsConflict()
    {
        var tenantId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var store = new InMemoryServiceRequestStore();
        store.Seed(new ServiceRequest(requestId, tenantId, Guid.NewGuid(), "Water leak in lobby"));

        await using var app = await BuildTestApplicationAsync(store);
        using var client = app.GetTestClient();

        using var request = CreateAuthenticatedRequest(
            method: HttpMethod.Patch,
            route: $"/api/v1/requests/{requestId}/status",
            role: "Customer",
            tenantId: tenantId,
            userId: Guid.NewGuid(),
            body: new TransitionServiceRequestStatusRequest
            {
                NextStatus = "Assigned",
                RowVersion = Convert.ToBase64String(new byte[] { 1, 2, 3 }),
            });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("CONCURRENCY_CONFLICT", body);
    }

    [Fact]
    public async Task TransitionStatus_DuplicateTargetStatus_ReturnsIdempotentSuccess()
    {
        var tenantId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var store = new InMemoryServiceRequestStore();

        var seeded = new ServiceRequest(requestId, tenantId, Guid.NewGuid(), "Water leak in lobby");
        seeded.TransitionTo(ServiceRequestStatus.Assigned);
        store.Seed(seeded);

        await using var app = await BuildTestApplicationAsync(store);
        using var client = app.GetTestClient();

        using var request = CreateAuthenticatedRequest(
            method: HttpMethod.Patch,
            route: $"/api/v1/requests/{requestId}/status",
            role: "Customer",
            tenantId: tenantId,
            userId: Guid.NewGuid(),
            body: new TransitionServiceRequestStatusRequest { NextStatus = "Assigned" });

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<TransitionServiceRequestStatusResponse>>();
        Assert.NotNull(envelope);
        Assert.True(envelope!.Success);
        Assert.NotNull(envelope.Data);
        Assert.Equal("Assigned", envelope.Data!.PreviousStatus);
        Assert.Equal("Assigned", envelope.Data.CurrentStatus);

        var stored = await store.GetByIdAsync(tenantId, requestId);
        Assert.NotNull(stored);
        Assert.Equal(ServiceRequestStatus.Assigned, stored!.Status);
    }

    [Fact]
    public async Task TransitionStatus_SlaEscalation_WritesAuditAndPublishesRealtimeEvent()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var store = new InMemoryServiceRequestStore();
        var request = new ServiceRequest(requestId, tenantId, userId, "Aged request");
        request.TransitionTo(ServiceRequestStatus.Assigned);

        var createdAtUtc = DateTime.UtcNow.AddHours(-5);
        typeof(ServiceRequest).GetProperty(nameof(ServiceRequest.CreatedAtUtc))!.SetValue(request, createdAtUtc);
        typeof(ServiceRequest).GetProperty(nameof(ServiceRequest.UpdatedAtUtc))!.SetValue(request, createdAtUtc);
        request.ApplySlaSnapshot(
            responseDueAtUtc: null,
            assignmentDueAtUtc: createdAtUtc.AddMinutes(30),
            completionDueAtUtc: createdAtUtc.AddMinutes(240),
            responseSlaState: SlaState.NotApplicable,
            assignmentSlaState: SlaState.OnTrack,
            completionSlaState: SlaState.OnTrack,
            nextSlaDeadlineAtUtc: createdAtUtc.AddMinutes(240));

        store.Seed(request);

        var auditWriter = new CapturingAuditLogWriter();
        var realtimePublisher = new CapturingOperationalUpdatePublisher();

        await using var app = await BuildTestApplicationAsync(store, auditWriter, realtimePublisher);
        using var client = app.GetTestClient();

        using var httpRequest = CreateAuthenticatedRequest(
            method: HttpMethod.Patch,
            route: $"/api/v1/requests/{requestId}/status",
            role: "Customer",
            tenantId: tenantId,
            userId: userId,
            body: new TransitionServiceRequestStatusRequest { NextStatus = "InProgress" });

        var response = await client.SendAsync(httpRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(auditWriter.Items, x => x.Action == "SlaEscalation:Completion:Breached");
        Assert.Contains(realtimePublisher.SlaEscalations, x => x.SlaDimension == "Completion" && x.CurrentSlaStatus == "Breached");
    }

    private static HttpRequestMessage CreateAuthenticatedRequest(
        HttpMethod method,
        string route,
        string role,
        Guid tenantId,
        Guid userId,
        TransitionServiceRequestStatusRequest body)
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
        InMemoryServiceRequestStore store,
        CapturingAuditLogWriter? auditWriter = null,
        CapturingOperationalUpdatePublisher? realtimePublisher = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddApplication();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IAuthenticatedPrincipalAccessor, HttpContextAuthenticatedPrincipalAccessor>();
        builder.Services.AddScoped<ITenantContextAccessor, HttpContextTenantContextAccessor>();

        builder.Services.AddScoped<IServiceRequestRepository>(_ => store);
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
            var query = this.items.Where(x => x.TenantId == specification.TenantId);

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

            IReadOnlyList<ServiceRequest> result = specification.SortBy switch
            {
                ServiceRequestSortField.Status when specification.SortDirection == SortDirection.Ascending => query.OrderBy(x => x.Status).ToList(),
                ServiceRequestSortField.Status => query.OrderByDescending(x => x.Status).ToList(),
                ServiceRequestSortField.Title when specification.SortDirection == SortDirection.Ascending => query.OrderBy(x => x.Title).ToList(),
                ServiceRequestSortField.Title => query.OrderByDescending(x => x.Title).ToList(),
                ServiceRequestSortField.CreatedAtUtc when specification.SortDirection == SortDirection.Ascending => query.OrderBy(x => x.CreatedAtUtc).ToList(),
                _ => query.OrderByDescending(x => x.CreatedAtUtc).ToList(),
            };

            return Task.FromResult(result);
        }

        public Task<int> CountAsync(ServiceRequestQuerySpecification specification, CancellationToken cancellationToken = default)
        {
            var query = this.items
                .Where(x => x.TenantId == specification.TenantId)
                .AsEnumerable();

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
            // Aggregate instance is already in-memory; no explicit action is required.
        }

        public void Remove(ServiceRequest aggregate)
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
