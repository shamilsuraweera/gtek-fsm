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
using GTEK.FSM.Backend.Application.Audit;
using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Backend.Application.Persistence.Transactions;
using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Domain.Audit;
using GTEK.FSM.Backend.Infrastructure.Identity;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Workers.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Workers.Responses;
using GTEK.FSM.Shared.Contracts.Results;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Integration;

public sealed class WorkerManagementIntegrationTests
{
    [Fact]
    public async Task GetWorkers_ManagerScope_ReturnsTenantWorkersOnly()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();

        var workerStore = new InMemoryWorkerProfileStore();
        workerStore.Seed(new WorkerProfile(Guid.NewGuid(), tenantId, "WRK-001", "Worker One", 4.2m, ["HVAC", "DISPATCH"]));
        workerStore.Seed(new WorkerProfile(Guid.NewGuid(), otherTenantId, "WRK-002", "Worker Two", 3.8m, ["PLUMBING"]));

        await using var app = await BuildTestApplicationAsync(workerStore);
        using var client = app.GetTestClient();

        using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/v1/management/workers?page=1&pageSize=20", "Manager", tenantId, Guid.NewGuid());
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<GetWorkersListResponse>>();
        Assert.NotNull(envelope);
        Assert.True(envelope!.Success);
        Assert.NotNull(envelope.Data);
        Assert.Single(envelope.Data!.Items);
        Assert.Equal("WRK-001", envelope.Data.Items[0].WorkerCode);
    }

    [Fact]
    public async Task CreateWorker_ManagerRole_CreatesWorkerAndWritesAudit()
    {
        var tenantId = Guid.NewGuid();

        var workerStore = new InMemoryWorkerProfileStore();
        var unitOfWork = new InMemoryUnitOfWork();
        var auditWriter = new InMemoryAuditLogWriter();

        await using var app = await BuildTestApplicationAsync(workerStore, unitOfWork, auditWriter);
        using var client = app.GetTestClient();

        var payload = new CreateWorkerProfileRequest
        {
            WorkerCode = "wrk-100",
            DisplayName = "Worker Dispatch",
            InternalRating = 4.5m,
            AvailabilityStatus = "Available",
            Skills = ["Dispatch", "Escalation"],
            IsActive = true,
        };

        using var request = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/management/workers", "Manager", tenantId, Guid.NewGuid());
        request.Content = JsonContent.Create(payload);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<WorkerProfileResponse>>();
        Assert.NotNull(envelope);
        Assert.True(envelope!.Success);
        Assert.NotNull(envelope.Data);
        Assert.Equal("WRK-100", envelope.Data!.WorkerCode);
        Assert.Equal(4.5m, envelope.Data.InternalRating);

        var workers = await workerStore.QueryAsync(new WorkerProfileQuerySpecification(tenantId, IncludeInactive: true), CancellationToken.None);
        Assert.Single(workers);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
        Assert.Contains(auditWriter.Entries, x => x.Action == "WORKER_PROFILE_CREATED" && x.TenantId == tenantId);
    }

    [Fact]
    public async Task CreateWorker_CustomerRole_ReturnsForbidden()
    {
        var tenantId = Guid.NewGuid();

        var workerStore = new InMemoryWorkerProfileStore();

        await using var app = await BuildTestApplicationAsync(workerStore);
        using var client = app.GetTestClient();

        var payload = new CreateWorkerProfileRequest
        {
            WorkerCode = "wrk-201",
            DisplayName = "Worker Forbidden",
            InternalRating = 3.5m,
            Skills = ["HVAC"],
        };

        using var request = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/management/workers", "Customer", tenantId, Guid.NewGuid());
        request.Content = JsonContent.Create(payload);

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
        InMemoryWorkerProfileStore workerStore,
        InMemoryUnitOfWork? unitOfWork = null,
        InMemoryAuditLogWriter? auditLogWriter = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        unitOfWork ??= new InMemoryUnitOfWork();
        auditLogWriter ??= new InMemoryAuditLogWriter();

        builder.Services.AddApplication();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IAuthenticatedPrincipalAccessor, HttpContextAuthenticatedPrincipalAccessor>();
        builder.Services.AddScoped<ITenantContextAccessor, HttpContextTenantContextAccessor>();

        builder.Services.AddScoped<IWorkerProfileRepository>(_ => workerStore);
        builder.Services.AddScoped<IUnitOfWork>(_ => unitOfWork);
        builder.Services.AddScoped<IAuditLogWriter>(_ => auditLogWriter);

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

    private sealed class InMemoryWorkerProfileStore : IWorkerProfileRepository
    {
        private readonly List<WorkerProfile> items = [];

        public void Seed(WorkerProfile workerProfile)
        {
            this.items.Add(workerProfile);
        }

        public Task AddAsync(WorkerProfile aggregate, CancellationToken cancellationToken = default)
        {
            this.items.Add(aggregate);
            return Task.CompletedTask;
        }

        public Task<WorkerProfile?> GetByIdAsync(Guid tenantId, Guid workerId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.items.FirstOrDefault(x => x.TenantId == tenantId && x.Id == workerId));
        }

        public Task<WorkerProfile?> GetForUpdateAsync(Guid tenantId, Guid workerId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.items.FirstOrDefault(x => x.TenantId == tenantId && x.Id == workerId));
        }

        public Task<WorkerProfile?> GetByCodeAsync(Guid tenantId, string workerCode, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.items.FirstOrDefault(x =>
                x.TenantId == tenantId
                && string.Equals(x.WorkerCode, workerCode, StringComparison.OrdinalIgnoreCase)));
        }

        public Task<IReadOnlyList<WorkerProfile>> QueryAsync(WorkerProfileQuerySpecification specification, CancellationToken cancellationToken = default)
        {
            var query = this.items.Where(x => x.TenantId == specification.TenantId).AsEnumerable();

            if (!specification.IncludeInactive)
            {
                query = query.Where(x => x.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(specification.SearchText))
            {
                query = query.Where(x =>
                    x.WorkerCode.Contains(specification.SearchText, StringComparison.OrdinalIgnoreCase)
                    || x.DisplayName.Contains(specification.SearchText, StringComparison.OrdinalIgnoreCase));
            }

            query = query.OrderBy(x => x.DisplayName);

            var page = specification.Page ?? new PageSpecification();
            return Task.FromResult<IReadOnlyList<WorkerProfile>>(query.Skip(page.Skip).Take(page.Take).ToList());
        }

        public Task<int> CountAsync(WorkerProfileQuerySpecification specification, CancellationToken cancellationToken = default)
        {
            var query = this.items.Where(x => x.TenantId == specification.TenantId).AsEnumerable();

            if (!specification.IncludeInactive)
            {
                query = query.Where(x => x.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(specification.SearchText))
            {
                query = query.Where(x =>
                    x.WorkerCode.Contains(specification.SearchText, StringComparison.OrdinalIgnoreCase)
                    || x.DisplayName.Contains(specification.SearchText, StringComparison.OrdinalIgnoreCase));
            }

            return Task.FromResult(query.Count());
        }

        public void Update(WorkerProfile aggregate)
        {
        }

        public void Remove(WorkerProfile aggregate)
        {
            this.items.Remove(aggregate);
        }
    }

    private sealed class InMemoryUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCount { get; private set; }

        public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IUnitOfWorkTransaction>(new InMemoryUnitOfWorkTransaction());
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            this.SaveChangesCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class InMemoryUnitOfWorkTransaction : IUnitOfWorkTransaction
    {
        public Guid? TransactionId => Guid.NewGuid();

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryAuditLogWriter : IAuditLogWriter
    {
        public List<AuditLog> Entries { get; } = [];

        public Task WriteAsync(AuditLog log, CancellationToken cancellationToken = default)
        {
            this.Entries.Add(log);
            return Task.CompletedTask;
        }
    }
}
