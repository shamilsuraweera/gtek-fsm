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
using GTEK.FSM.Backend.Infrastructure.Identity;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Audit.Responses;
using GTEK.FSM.Shared.Contracts.Results;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Integration;

public class AuditLogQueryIntegrationTests
{
    [Fact]
    public async Task GetAuditLogs_ManagerScope_AppliesTenantAndFilterBoundaries()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var fromUtc = DateTimeOffset.UtcNow.AddHours(-2);
        var toUtc = DateTimeOffset.UtcNow.AddHours(2);

        var auditStore = new InMemoryAuditLogStore();
        auditStore.Seed(new Domain.Audit.AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ActorUserId = actorUserId,
            EntityType = "ServiceRequest",
            EntityId = entityId,
            Action = "CATEGORY_UPDATED",
            Outcome = "Success",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            Details = "Included row",
        });
        auditStore.Seed(new Domain.Audit.AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ActorUserId = actorUserId,
            EntityType = "ServiceRequest",
            EntityId = entityId,
            Action = "CATEGORY_UPDATED",
            Outcome = "Failure",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            Details = "Wrong outcome",
        });
        auditStore.Seed(new Domain.Audit.AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = otherTenantId,
            ActorUserId = actorUserId,
            EntityType = "ServiceRequest",
            EntityId = entityId,
            Action = "CATEGORY_UPDATED",
            Outcome = "Success",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            Details = "Other tenant row",
        });
        auditStore.Seed(new Domain.Audit.AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ActorUserId = actorUserId,
            EntityType = "ServiceCategory",
            EntityId = Guid.NewGuid(),
            Action = "CATEGORY_CREATED",
            Outcome = "Success",
            OccurredAtUtc = DateTimeOffset.UtcNow.AddDays(-3),
            Details = "Outside time range",
        });

        await using var app = await BuildTestApplicationAsync(auditStore);
        using var client = app.GetTestClient();

        var route = $"/api/v1/management/audit-logs?actorUserId={actorUserId}&entityType=ServiceRequest&entityId={entityId}&action=CATEGORY&outcome=Success&fromUtc={Uri.EscapeDataString(fromUtc.ToString("O"))}&toUtc={Uri.EscapeDataString(toUtc.ToString("O"))}&page=1&pageSize=10";
        using var request = CreateAuthenticatedRequest(HttpMethod.Get, route, "Manager", tenantId, Guid.NewGuid());

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<GetAuditLogsListResponse>>();
        Assert.NotNull(envelope);
        Assert.True(envelope!.Success);
        Assert.NotNull(envelope.Data);
        Assert.Single(envelope.Data!.Items);
        Assert.Equal("CATEGORY_UPDATED", envelope.Data.Items[0].Action);
        Assert.Equal("Success", envelope.Data.Items[0].Outcome);
        Assert.Equal(1, envelope.Data.Pagination.Total);
    }

    [Theory]
    [InlineData("Customer")]
    [InlineData("Worker")]
    [InlineData("Support")]
    public async Task GetAuditLogs_NonManagementRoles_ReturnForbidden(string role)
    {
        var tenantId = Guid.NewGuid();
        var auditStore = new InMemoryAuditLogStore();
        await using var app = await BuildTestApplicationAsync(auditStore);
        using var client = app.GetTestClient();

        using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/v1/management/audit-logs?page=1&pageSize=10", role, tenantId, Guid.NewGuid());
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_InvalidDateRange_ReturnsBadRequest()
    {
        var tenantId = Guid.NewGuid();
        var auditStore = new InMemoryAuditLogStore();
        await using var app = await BuildTestApplicationAsync(auditStore);
        using var client = app.GetTestClient();

        var route = $"/api/v1/management/audit-logs?fromUtc={Uri.EscapeDataString(DateTimeOffset.UtcNow.ToString("O"))}&toUtc={Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(-1).ToString("O"))}";
        using var request = CreateAuthenticatedRequest(HttpMethod.Get, route, "Manager", tenantId, Guid.NewGuid());
        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("VALIDATION_FAILED", body);
    }

    [Fact]
    public async Task ExportAuditLogs_ManagerScope_ReturnsTenantScopedCsv()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();

        var auditStore = new InMemoryAuditLogStore();
        auditStore.Seed(new Domain.Audit.AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ActorUserId = Guid.NewGuid(),
            EntityType = "ServiceCategory",
            EntityId = Guid.NewGuid(),
            Action = "CATEGORY_REORDERED",
            Outcome = "Success",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            Details = "First tenant row",
        });
        auditStore.Seed(new Domain.Audit.AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = otherTenantId,
            ActorUserId = Guid.NewGuid(),
            EntityType = "ServiceCategory",
            EntityId = Guid.NewGuid(),
            Action = "CATEGORY_REORDERED",
            Outcome = "Success",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            Details = "Other tenant row",
        });

        await using var app = await BuildTestApplicationAsync(auditStore);
        using var client = app.GetTestClient();

        using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/v1/management/audit-logs/export?action=REORDERED", "Admin", tenantId, Guid.NewGuid());
        var response = await client.SendAsync(request);
        var csv = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("auditLogId,tenantId,actorUserId,entityType,entityId,action,outcome,occurredAtUtc,details", csv);
        Assert.Contains("CATEGORY_REORDERED", csv);
        Assert.Contains("First tenant row", csv);
        Assert.DoesNotContain("Other tenant row", csv);
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

    private static async Task<WebApplication> BuildTestApplicationAsync(InMemoryAuditLogStore auditStore)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddApplication();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IAuthenticatedPrincipalAccessor, HttpContextAuthenticatedPrincipalAccessor>();
        builder.Services.AddScoped<ITenantContextAccessor, HttpContextTenantContextAccessor>();
        builder.Services.AddScoped<IAuditLogRepository>(_ => auditStore);
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
            if (!Request.Headers.TryGetValue("Authorization", out var authValues) || authValues.Count == 0)
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

    private sealed class InMemoryAuditLogStore : IAuditLogRepository
    {
        private readonly List<Domain.Audit.AuditLog> items = new();

        public void Seed(Domain.Audit.AuditLog auditLog)
        {
            this.items.Add(auditLog);
        }

        public Task<IReadOnlyList<Domain.Audit.AuditLog>> QueryAsync(AuditLogQuerySpecification specification, CancellationToken cancellationToken = default)
        {
            var filtered = ApplyFilters(specification)
                .OrderByDescending(x => x.OccurredAtUtc)
                .ThenByDescending(x => x.Id)
                .ToList();

            var page = specification.Page ?? new PageSpecification();
            return Task.FromResult<IReadOnlyList<Domain.Audit.AuditLog>>(filtered.Skip(page.Skip).Take(page.Take).ToList());
        }

        public Task<int> CountAsync(AuditLogQuerySpecification specification, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ApplyFilters(specification).Count());
        }

        private IEnumerable<Domain.Audit.AuditLog> ApplyFilters(AuditLogQuerySpecification specification)
        {
            var query = this.items.Where(x => x.TenantId == specification.TenantId);

            if (specification.ActorUserId.HasValue)
            {
                query = query.Where(x => x.ActorUserId == specification.ActorUserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(specification.EntityType))
            {
                query = query.Where(x => x.EntityType == specification.EntityType);
            }

            if (specification.EntityId.HasValue)
            {
                query = query.Where(x => x.EntityId == specification.EntityId.Value);
            }

            if (!string.IsNullOrWhiteSpace(specification.Action))
            {
                query = query.Where(x => x.Action.Contains(specification.Action, StringComparison.Ordinal));
            }

            if (!string.IsNullOrWhiteSpace(specification.Outcome))
            {
                query = query.Where(x => x.Outcome == specification.Outcome);
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
}
