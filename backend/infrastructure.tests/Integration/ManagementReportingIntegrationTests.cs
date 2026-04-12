using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using GTEK.FSM.Backend.Api.Authorization;
using GTEK.FSM.Backend.Api.Authentication;
using GTEK.FSM.Backend.Api.Middleware;
using GTEK.FSM.Backend.Api.Routing;
using GTEK.FSM.Backend.Api.Tenancy;
using GTEK.FSM.Backend.Application;
using GTEK.FSM.Backend.Application.Decisioning;
using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Backend.Domain.Audit;
using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Backend.Infrastructure.Identity;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Reports.Responses;
using GTEK.FSM.Shared.Contracts.Results;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Integration;

public class ManagementReportingIntegrationTests
{
    [Fact]
    public async Task GetManagementReportsOverview_ManagerRole_ReturnsTenantScopedAnalytics()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();

        var requestStore = new InMemoryServiceRequestStore();
        requestStore.Seed(tenantId, ServiceRequestStatus.New, DateTime.UtcNow.AddDays(-1));
        requestStore.Seed(tenantId, ServiceRequestStatus.Completed, DateTime.UtcNow.AddDays(-1), completionSlaState: SlaState.Breached);
        requestStore.Seed(otherTenantId, ServiceRequestStatus.New, DateTime.UtcNow.AddDays(-1));

        var jobStore = new InMemoryJobStore();
        var busyWorkerId = Guid.NewGuid();
        var overloadedWorkerId = Guid.NewGuid();
        jobStore.Seed(tenantId, AssignmentStatus.Accepted, busyWorkerId, DateTime.UtcNow.AddDays(-1));
        jobStore.Seed(tenantId, AssignmentStatus.Accepted, overloadedWorkerId, DateTime.UtcNow.AddDays(-1));
        jobStore.Seed(tenantId, AssignmentStatus.PendingAcceptance, overloadedWorkerId, DateTime.UtcNow.AddDays(-1));
        jobStore.Seed(tenantId, AssignmentStatus.Completed, busyWorkerId, DateTime.UtcNow.AddDays(-1));
        jobStore.Seed(otherTenantId, AssignmentStatus.Accepted, Guid.NewGuid(), DateTime.UtcNow.AddDays(-1));

        var workerStore = new InMemoryWorkerProfileStore();
        workerStore.Seed(tenantId, Guid.NewGuid(), "W-001", 4.9m, WorkerAvailabilityStatus.Available);
        workerStore.Seed(tenantId, busyWorkerId, "W-002", 4.5m, WorkerAvailabilityStatus.Busy);
        workerStore.Seed(tenantId, overloadedWorkerId, "W-003", 4.2m, WorkerAvailabilityStatus.Available);
        workerStore.Seed(otherTenantId, Guid.NewGuid(), "W-999", 4.1m, WorkerAvailabilityStatus.Available);

        var auditStore = new InMemoryAuditLogStore();
        auditStore.Seed(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Action = $"AssignWorker:{busyWorkerId}",
            Outcome = "Success",
            OccurredAtUtc = DateTimeOffset.UtcNow.AddHours(-3),
            EntityType = "ServiceRequest",
            EntityId = Guid.NewGuid(),
        });
        auditStore.Seed(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Action = $"AssignWorker:{overloadedWorkerId}",
            Outcome = "Success",
            OccurredAtUtc = DateTimeOffset.UtcNow.AddHours(-2),
            EntityType = "ServiceRequest",
            EntityId = Guid.NewGuid(),
        });
        auditStore.Seed(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Action = $"AssignWorker:{overloadedWorkerId}",
            Outcome = "Success",
            OccurredAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
            EntityType = "ServiceRequest",
            EntityId = Guid.NewGuid(),
        });
        auditStore.Seed(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Action = "REQUEST_COMPLETED",
            Outcome = "Success",
            OccurredAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
            EntityType = "ServiceRequest",
            EntityId = Guid.NewGuid(),
        });
        auditStore.Seed(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Action = "SlaEscalation:Completion:Breached",
            Outcome = "Success",
            OccurredAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
            EntityType = "ServiceRequest",
            EntityId = Guid.NewGuid(),
        });
        auditStore.Seed(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Action = "POLICY_OVERRIDE",
            Outcome = "Denied",
            OccurredAtUtc = DateTimeOffset.UtcNow.AddHours(-2),
            EntityType = "Subscription",
            EntityId = Guid.NewGuid(),
        });
        auditStore.Seed(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = otherTenantId,
            Action = "REQUEST_COMPLETED",
            Outcome = "Success",
            OccurredAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
            EntityType = "ServiceRequest",
            EntityId = Guid.NewGuid(),
        });

        var metricsCollector = new SeededDecisioningMetricsCollector();
        metricsCollector.RecordMatchEvaluation(tenantId, DateTime.UtcNow.AddMinutes(-30), 100, 4, 0.92m);
        metricsCollector.RecordMatchEvaluation(tenantId, DateTime.UtcNow.AddMinutes(-20), 200, 5, 0.84m);
        metricsCollector.RecordMatchEvaluation(tenantId, DateTime.UtcNow.AddMinutes(-10), 300, 3, 0.74m);

        await using var app = await BuildTestApplicationAsync(requestStore, jobStore, workerStore, auditStore, metricsCollector);
        using var client = app.GetTestClient();

        using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/v1/management/reports/overview?windowDays=7&trendBuckets=7", "Manager", tenantId, Guid.NewGuid());
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<GetManagementAnalyticsOverviewResponse>>();
        Assert.NotNull(envelope);
        Assert.True(envelope!.Success);
        Assert.NotNull(envelope.Data);
        Assert.Equal(2, envelope.Data!.TotalRequestsInWindow);
        Assert.Equal(1, envelope.Data.CompletedRequestsInWindow);
        Assert.Equal(2, envelope.Data.ActiveJobs);
        Assert.Equal(6, envelope.Data.SensitiveActions24h);
        Assert.Equal(1, envelope.Data.DeniedActions24h);
        Assert.Equal(3, envelope.Data.DecisioningMetrics.MatchEvaluationCount);
        Assert.Equal(200m, envelope.Data.DecisioningMetrics.AverageMatchLatencyMs);
        Assert.True(envelope.Data.DecisioningMetrics.P95MatchLatencyMs >= 200m);
        Assert.Equal(1, envelope.Data.DecisioningMetrics.SlaOutcomes.CompletionBreached);
        Assert.Equal(1, envelope.Data.DecisioningMetrics.SlaOutcomes.EscalationsBreachedInWindow);
        Assert.Equal(3, envelope.Data.AssignmentQuality.AssignmentEventsInWindow);
        Assert.Equal(2, envelope.Data.AssignmentQuality.AcceptedJobs);
        Assert.Equal(1, envelope.Data.AssignmentQuality.PendingAcceptanceJobs);
        Assert.Equal(1, envelope.Data.AssignmentQuality.CompletedJobs);
        Assert.Equal(3, envelope.Data.WorkforceUtilization.ActiveWorkers);
        Assert.Equal(2, envelope.Data.WorkforceUtilization.AvailableWorkers);
        Assert.Equal(1, envelope.Data.WorkforceUtilization.BusyWorkers);
        Assert.Equal(2, envelope.Data.WorkforceUtilization.UtilizedWorkers);
        Assert.Equal(1, envelope.Data.WorkforceUtilization.OverloadedWorkers);
    }

    [Fact]
    public async Task GetManagementReportsOverview_CustomerRole_ReturnsForbidden()
    {
        var tenantId = Guid.NewGuid();

        await using var app = await BuildTestApplicationAsync(
            new InMemoryServiceRequestStore(),
            new InMemoryJobStore(),
            new InMemoryWorkerProfileStore(),
            new InMemoryAuditLogStore(),
            null);
        using var client = app.GetTestClient();

        using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/v1/management/reports/overview", "Customer", tenantId, Guid.NewGuid());
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
        InMemoryServiceRequestStore requestStore,
        InMemoryJobStore jobStore,
        InMemoryWorkerProfileStore workerStore,
        InMemoryAuditLogStore auditStore,
        IDecisioningMetricsCollector? metricsCollector)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddApplication();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IAuthenticatedPrincipalAccessor, HttpContextAuthenticatedPrincipalAccessor>();
        builder.Services.AddScoped<ITenantContextAccessor, HttpContextTenantContextAccessor>();
        builder.Services.AddScoped<IServiceRequestRepository>(_ => requestStore);
        builder.Services.AddScoped<IJobRepository>(_ => jobStore);
        builder.Services.AddScoped<IWorkerProfileRepository>(_ => workerStore);
        builder.Services.AddScoped<IAuditLogRepository>(_ => auditStore);
        builder.Services.AddScoped<ILocalAuthService, StubLocalAuthService>();
        if (metricsCollector is not null)
        {
            builder.Services.AddSingleton<IDecisioningMetricsCollector>(_ => metricsCollector);
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

    private sealed class StubLocalAuthService : ILocalAuthService
    {
        public Task<LocalAuthResult> LoginAsync(GTEK.FSM.Shared.Contracts.Api.Contracts.Auth.Requests.LoginRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<LocalAuthResult> RegisterAsync(GTEK.FSM.Shared.Contracts.Api.Contracts.Auth.Requests.RegisterLocalUserRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
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
        public const string SchemeName = "TestAuth";

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!this.Request.Headers.TryGetValue(TestAuthHeaders.Subject, out var subject)
                || !Guid.TryParse(subject.ToString(), out var userId)
                || userId == Guid.Empty)
            {
                return Task.FromResult(AuthenticateResult.Fail("Missing or invalid test subject."));
            }

            if (!this.Request.Headers.TryGetValue(TestAuthHeaders.TenantId, out var tenant)
                || !Guid.TryParse(tenant.ToString(), out var tenantId)
                || tenantId == Guid.Empty)
            {
                return Task.FromResult(AuthenticateResult.Fail("Missing or invalid test tenant."));
            }

            var role = this.Request.Headers.TryGetValue(TestAuthHeaders.Role, out var roleHeader)
                ? roleHeader.ToString()
                : string.Empty;

            var claims = new List<Claim>
            {
                new("sub", userId.ToString()),
                new("tenant_id", tenantId.ToString()),
            };

            if (!string.IsNullOrWhiteSpace(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
                claims.Add(new Claim("role", role));
            }

            if (this.Request.Headers.TryGetValue(TestAuthHeaders.TokenVersion, out var versionHeader)
                && !string.IsNullOrWhiteSpace(versionHeader))
            {
                claims.Add(new Claim("ver", versionHeader.ToString()));
            }

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private sealed class InMemoryServiceRequestStore : IServiceRequestRepository
    {
        private readonly List<RequestRow> rows = [];

        public void Seed(
            Guid tenantId,
            ServiceRequestStatus status,
            DateTime createdAtUtc,
            SlaState responseSlaState = SlaState.OnTrack,
            SlaState assignmentSlaState = SlaState.OnTrack,
            SlaState completionSlaState = SlaState.OnTrack)
        {
            this.rows.Add(new RequestRow(
                TenantId: tenantId,
                Status: status,
                CreatedAtUtc: createdAtUtc,
                ResponseSlaState: responseSlaState,
                AssignmentSlaState: assignmentSlaState,
                CompletionSlaState: completionSlaState));
        }

        public Task<int> CountAsync(ServiceRequestQuerySpecification specification, CancellationToken cancellationToken = default)
        {
            var query = this.rows.Where(x => x.TenantId == specification.TenantId);

            if (specification.Status.HasValue)
            {
                query = query.Where(x => x.Status == specification.Status.Value);
            }

            if (specification.CreatedFromUtc.HasValue)
            {
                query = query.Where(x => x.CreatedAtUtc >= specification.CreatedFromUtc.Value);
            }

            if (specification.CreatedToUtc.HasValue)
            {
                query = query.Where(x => x.CreatedAtUtc <= specification.CreatedToUtc.Value);
            }

            return Task.FromResult(query.Count());
        }

        public Task AddAsync(ServiceRequest aggregate, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<ServiceRequest?> GetByIdAsync(Guid tenantId, Guid requestId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<ServiceRequest?> GetForUpdateAsync(Guid tenantId, Guid requestId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<IReadOnlyList<ServiceRequest>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<IReadOnlyList<ServiceRequest>> ListByCustomerAsync(Guid tenantId, Guid customerUserId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<IReadOnlyList<ServiceRequest>> QueryAsync(ServiceRequestQuerySpecification specification, CancellationToken cancellationToken = default)
        {
            var query = this.rows.Where(x => x.TenantId == specification.TenantId);

            if (specification.Status.HasValue)
            {
                query = query.Where(x => x.Status == specification.Status.Value);
            }

            if (specification.CreatedFromUtc.HasValue)
            {
                query = query.Where(x => x.CreatedAtUtc >= specification.CreatedFromUtc.Value);
            }

            if (specification.CreatedToUtc.HasValue)
            {
                query = query.Where(x => x.CreatedAtUtc <= specification.CreatedToUtc.Value);
            }

            query = specification.SortDirection == SortDirection.Ascending
                ? query.OrderBy(x => x.CreatedAtUtc)
                : query.OrderByDescending(x => x.CreatedAtUtc);

            var page = specification.Page ?? new PageSpecification();
            IReadOnlyList<ServiceRequest> result = query
                .Skip(page.Skip)
                .Take(page.Take)
                .Select(ToAggregate)
                .ToArray();

            return Task.FromResult(result);
        }

        public void Remove(ServiceRequest aggregate) => throw new NotImplementedException();

        public void Update(ServiceRequest aggregate) => throw new NotImplementedException();

        private static ServiceRequest ToAggregate(RequestRow row)
        {
            var aggregate = new ServiceRequest(Guid.NewGuid(), row.TenantId, Guid.NewGuid(), "report-row");
            typeof(ServiceRequest).GetProperty(nameof(ServiceRequest.Status))!.SetValue(aggregate, row.Status);
            typeof(ServiceRequest).GetProperty(nameof(ServiceRequest.CreatedAtUtc))!.SetValue(aggregate, row.CreatedAtUtc);
            typeof(ServiceRequest).GetProperty(nameof(ServiceRequest.UpdatedAtUtc))!.SetValue(aggregate, row.CreatedAtUtc);

            aggregate.ApplySlaSnapshot(
                responseDueAtUtc: row.CreatedAtUtc.AddMinutes(15),
                assignmentDueAtUtc: row.CreatedAtUtc.AddMinutes(30),
                completionDueAtUtc: row.CreatedAtUtc.AddMinutes(240),
                responseSlaState: row.ResponseSlaState,
                assignmentSlaState: row.AssignmentSlaState,
                completionSlaState: row.CompletionSlaState,
                nextSlaDeadlineAtUtc: row.CreatedAtUtc.AddMinutes(15));

            return aggregate;
        }

        private sealed record RequestRow(
            Guid TenantId,
            ServiceRequestStatus Status,
            DateTime CreatedAtUtc,
            SlaState ResponseSlaState,
            SlaState AssignmentSlaState,
            SlaState CompletionSlaState);
    }

    private sealed class InMemoryJobStore : IJobRepository
    {
        private readonly List<JobRow> rows = [];

        public void Seed(Guid tenantId, AssignmentStatus status, Guid? workerId = null, DateTime? createdAtUtc = null)
        {
            this.rows.Add(new JobRow(Guid.NewGuid(), tenantId, status, workerId, createdAtUtc ?? DateTime.UtcNow));
        }

        public Task<int> CountAsync(JobQuerySpecification specification, CancellationToken cancellationToken = default)
        {
            var query = this.rows.Where(x => x.TenantId == specification.TenantId);
            if (specification.AssignmentStatus.HasValue)
            {
                query = query.Where(x => x.Status == specification.AssignmentStatus.Value);
            }

            return Task.FromResult(query.Count());
        }

        public Task AddAsync(Job aggregate, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<Job?> GetByIdAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<Job?> GetForUpdateAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<IReadOnlyList<Job>> ListByServiceRequestAsync(Guid tenantId, Guid serviceRequestId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<IReadOnlyList<Job>> ListByWorkerAsync(Guid tenantId, Guid workerUserId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<IReadOnlyList<Job>> QueryAsync(JobQuerySpecification specification, CancellationToken cancellationToken = default)
        {
            var query = this.rows.Where(x => x.TenantId == specification.TenantId);

            if (specification.AssignmentStatus.HasValue)
            {
                query = query.Where(x => x.Status == specification.AssignmentStatus.Value);
            }

            if (specification.AssignedWorkerUserId.HasValue)
            {
                query = query.Where(x => x.AssignedWorkerUserId == specification.AssignedWorkerUserId.Value);
            }

            if (specification.ScheduledFromUtc.HasValue)
            {
                query = query.Where(x => x.CreatedAtUtc >= specification.ScheduledFromUtc.Value);
            }

            if (specification.ScheduledToUtc.HasValue)
            {
                query = query.Where(x => x.CreatedAtUtc <= specification.ScheduledToUtc.Value);
            }

            query = specification.SortDirection == SortDirection.Ascending
                ? query.OrderBy(x => x.CreatedAtUtc)
                : query.OrderByDescending(x => x.CreatedAtUtc);

            var page = specification.Page ?? new PageSpecification();
            IReadOnlyList<Job> result = query
                .Skip(page.Skip)
                .Take(page.Take)
                .Select(ToAggregate)
                .ToArray();

            return Task.FromResult(result);
        }

        public Task<IReadOnlyDictionary<Guid, int>> GetActiveJobCountsByWorkerAsync(
            Guid tenantId, IReadOnlyList<Guid> workerIds, CancellationToken cancellationToken = default)
        {
            IReadOnlyDictionary<Guid, int> result = this.rows
                .Where(x => x.TenantId == tenantId
                    && x.AssignedWorkerUserId.HasValue
                    && workerIds.Contains(x.AssignedWorkerUserId.Value)
                    && (x.Status == AssignmentStatus.PendingAcceptance || x.Status == AssignmentStatus.Accepted))
                .GroupBy(x => x.AssignedWorkerUserId!.Value)
                .ToDictionary(x => x.Key, x => x.Count());

            return Task.FromResult(result);
        }

        public void Remove(Job aggregate) => throw new NotImplementedException();

        public void Update(Job aggregate) => throw new NotImplementedException();

        private static Job ToAggregate(JobRow row)
        {
            var aggregate = new Job(row.Id, row.TenantId, Guid.NewGuid());
            typeof(Job).GetProperty(nameof(Job.CreatedAtUtc))!.SetValue(aggregate, row.CreatedAtUtc);
            typeof(Job).GetProperty(nameof(Job.UpdatedAtUtc))!.SetValue(aggregate, row.CreatedAtUtc);
            typeof(Job).GetProperty(nameof(Job.AssignmentStatus))!.SetValue(aggregate, row.Status);
            typeof(Job).GetProperty(nameof(Job.AssignedWorkerUserId))!.SetValue(aggregate, row.AssignedWorkerUserId);
            return aggregate;
        }

        private sealed record JobRow(Guid Id, Guid TenantId, AssignmentStatus Status, Guid? AssignedWorkerUserId, DateTime CreatedAtUtc);
    }

    private sealed class InMemoryWorkerProfileStore : IWorkerProfileRepository
    {
        private readonly List<WorkerProfile> items = [];

        public void Seed(Guid tenantId, Guid workerId, string workerCode, decimal rating, WorkerAvailabilityStatus availabilityStatus, bool isActive = true)
        {
            var worker = new WorkerProfile(workerId, tenantId, workerCode, workerCode, rating);
            worker.SetAvailability(availabilityStatus);
            if (!isActive)
            {
                worker.Deactivate();
            }

            this.items.Add(worker);
        }

        public Task<WorkerProfile?> GetByIdAsync(Guid tenantId, Guid workerId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.items.FirstOrDefault(x => x.TenantId == tenantId && x.Id == workerId));
        }

        public Task<WorkerProfile?> GetForUpdateAsync(Guid tenantId, Guid workerId, CancellationToken cancellationToken = default) => GetByIdAsync(tenantId, workerId, cancellationToken);

        public Task<WorkerProfile?> GetByCodeAsync(Guid tenantId, string workerCode, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.items.FirstOrDefault(x => x.TenantId == tenantId && x.WorkerCode == workerCode));
        }

        public Task<IReadOnlyList<WorkerProfile>> QueryAsync(WorkerProfileQuerySpecification specification, CancellationToken cancellationToken = default)
        {
            var query = this.items.Where(x => x.TenantId == specification.TenantId);

            if (!specification.IncludeInactive)
            {
                query = query.Where(x => x.IsActive);
            }

            var page = specification.Page ?? new PageSpecification();
            IReadOnlyList<WorkerProfile> result = query
                .OrderBy(x => x.DisplayName)
                .Skip(page.Skip)
                .Take(page.Take)
                .ToArray();

            return Task.FromResult(result);
        }

        public Task<int> CountAsync(WorkerProfileQuerySpecification specification, CancellationToken cancellationToken = default)
        {
            var query = this.items.Where(x => x.TenantId == specification.TenantId);
            if (!specification.IncludeInactive)
            {
                query = query.Where(x => x.IsActive);
            }

            return Task.FromResult(query.Count());
        }

        public Task AddAsync(WorkerProfile aggregate, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public void Remove(WorkerProfile aggregate) => throw new NotImplementedException();

        public void Update(WorkerProfile aggregate) => throw new NotImplementedException();
    }

    private sealed class InMemoryAuditLogStore : IAuditLogRepository
    {
        private readonly List<AuditLog> items = [];

        public void Seed(AuditLog auditLog)
        {
            this.items.Add(auditLog);
        }

        public Task<int> CountAsync(AuditLogQuerySpecification specification, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Apply(specification).Count());
        }

        public Task<IReadOnlyList<AuditLog>> QueryAsync(AuditLogQuerySpecification specification, CancellationToken cancellationToken = default)
        {
            var query = Apply(specification)
                .OrderByDescending(x => x.OccurredAtUtc)
                .ThenByDescending(x => x.Id);

            var page = specification.Page ?? new PageSpecification();
            IReadOnlyList<AuditLog> result = query.Skip(page.Skip).Take(page.Take).ToArray();
            return Task.FromResult(result);
        }

        private IEnumerable<AuditLog> Apply(AuditLogQuerySpecification specification)
        {
            var query = this.items.Where(x => x.TenantId == specification.TenantId);

            if (!string.IsNullOrWhiteSpace(specification.Action))
            {
                query = query.Where(x => x.Action.Contains(specification.Action, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(specification.Outcome))
            {
                query = query.Where(x => string.Equals(x.Outcome, specification.Outcome, StringComparison.OrdinalIgnoreCase));
            }

            if (specification.OccurredFromUtc.HasValue)
            {
                query = query.Where(x => x.OccurredAtUtc >= specification.OccurredFromUtc.Value);
            }

            if (specification.OccurredToUtc.HasValue)
            {
                query = query.Where(x => x.OccurredAtUtc <= specification.OccurredToUtc.Value);
            }

            return query;
        }
    }

    private sealed class SeededDecisioningMetricsCollector : IDecisioningMetricsCollector
    {
        private readonly List<DecisioningMatchMetricSample> samples = [];

        public void RecordMatchEvaluation(
            Guid tenantId,
            DateTime observedAtUtc,
            long matchLatencyMs,
            int candidateCount,
            decimal? topCandidateScore)
        {
            this.samples.Add(new DecisioningMatchMetricSample(
                TenantId: tenantId,
                ObservedAtUtc: observedAtUtc,
                MatchLatencyMs: matchLatencyMs,
                CandidateCount: candidateCount,
                TopCandidateScore: topCandidateScore));
        }

        public IReadOnlyList<DecisioningMatchMetricSample> GetMatchEvaluations(Guid tenantId, DateTime fromUtc, DateTime toUtc)
        {
            return this.samples
                .Where(x => x.TenantId == tenantId && x.ObservedAtUtc >= fromUtc && x.ObservedAtUtc <= toUtc)
                .ToArray();
        }
    }
}
