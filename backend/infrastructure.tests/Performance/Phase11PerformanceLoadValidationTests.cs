using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Channels;

using GTEK.FSM.Backend.Api.Authentication;
using GTEK.FSM.Backend.Api.Authorization;
using GTEK.FSM.Backend.Api.Middleware;
using GTEK.FSM.Backend.Api.Realtime;
using GTEK.FSM.Backend.Api.Routing;
using GTEK.FSM.Backend.Api.Tenancy;
using GTEK.FSM.Backend.Application;
using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Backend.Application.Persistence.Transactions;
using GTEK.FSM.Backend.Application.Realtime;
using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Backend.Infrastructure.Identity;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Auth.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Realtime;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Requests;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Performance;

public class Phase11PerformanceLoadValidationTests
{
    private const int QueueReadSampleCount = 240;
    private const int LifecycleSampleCount = 120;
    private const int RealtimeSampleCount = 36;
    private const int RealtimeSubscriberCount = 6;

    [Fact]
    public async Task QueueRead_LoadBudget_MeetsSlo()
    {
        var tenantId = Guid.NewGuid();
        var requestStore = new InMemoryServiceRequestStore();
        var jobStore = new InMemoryJobStore();
        var userStore = new InMemoryUserStore();

        for (var i = 0; i < 800; i++)
        {
            var request = new ServiceRequest(Guid.NewGuid(), tenantId, Guid.NewGuid(), $"Queue benchmark request {i}");
            if (i % 3 == 0)
            {
                request.TransitionTo(ServiceRequestStatus.Assigned);
            }

            requestStore.Seed(request);
        }

        await using var app = await BuildTestApplicationAsync(requestStore, jobStore, userStore, enableRealtimePublisher: false);
        using var client = app.GetTestClient();

        var latenciesMs = new List<double>(QueueReadSampleCount);

        for (var i = 0; i < 20; i++)
        {
            using var warmup = CreateAuthenticatedRequest(
                HttpMethod.Get,
                "/api/v1/requests?page=1&pageSize=25",
                "Support",
                tenantId,
                Guid.NewGuid());
            _ = await client.SendAsync(warmup);
        }

        var totalStopwatch = Stopwatch.StartNew();
        for (var i = 0; i < QueueReadSampleCount; i++)
        {
            using var request = CreateAuthenticatedRequest(
                HttpMethod.Get,
                "/api/v1/requests?page=1&pageSize=25&sortBy=updatedAtUtc&sortDirection=desc",
                "Support",
                tenantId,
                Guid.NewGuid());

            var startTicks = Stopwatch.GetTimestamp();
            var response = await client.SendAsync(request);
            var elapsed = ElapsedMilliseconds(startTicks);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            latenciesMs.Add(elapsed);
        }

        totalStopwatch.Stop();
        var metrics = BuildMetrics(latenciesMs, totalStopwatch.Elapsed.TotalSeconds);

        Console.WriteLine($"QUEUE_READ: samples={QueueReadSampleCount}, p50={metrics.P50Ms:F2}ms, p95={metrics.P95Ms:F2}ms, max={metrics.MaxMs:F2}ms, throughput={metrics.ThroughputOpsPerSec:F2} req/s");

        Assert.True(metrics.P95Ms <= 120, $"Queue read p95 exceeded SLO: {metrics.P95Ms:F2}ms > 120ms");
        Assert.True(metrics.ThroughputOpsPerSec >= 55, $"Queue read throughput below SLO: {metrics.ThroughputOpsPerSec:F2} req/s < 55 req/s");
    }

    [Fact]
    public async Task LifecycleWrite_LoadBudget_MeetsSlo()
    {
        var tenantId = Guid.NewGuid();
        var requestStore = new InMemoryServiceRequestStore();
        var jobStore = new InMemoryJobStore();
        var userStore = new InMemoryUserStore();

        var requestIds = new List<Guid>(LifecycleSampleCount + 16);
        for (var i = 0; i < LifecycleSampleCount + 16; i++)
        {
            var id = Guid.NewGuid();
            requestIds.Add(id);
            requestStore.Seed(new ServiceRequest(id, tenantId, Guid.NewGuid(), $"Lifecycle benchmark request {i}"));
        }

        await using var app = await BuildTestApplicationAsync(requestStore, jobStore, userStore, enableRealtimePublisher: false);
        using var client = app.GetTestClient();

        var latenciesMs = new List<double>(LifecycleSampleCount);

        for (var i = 0; i < 12; i++)
        {
            using var warmup = CreateAuthenticatedRequest(
                HttpMethod.Patch,
                $"/api/v1/requests/{requestIds[i]}/status",
                "Customer",
                tenantId,
                Guid.NewGuid(),
                new TransitionServiceRequestStatusRequest { NextStatus = "Assigned" });
            _ = await client.SendAsync(warmup);
        }

        var totalStopwatch = Stopwatch.StartNew();
        for (var i = 0; i < LifecycleSampleCount; i++)
        {
            using var request = CreateAuthenticatedRequest(
                HttpMethod.Patch,
                $"/api/v1/requests/{requestIds[i + 12]}/status",
                "Customer",
                tenantId,
                Guid.NewGuid(),
                new TransitionServiceRequestStatusRequest { NextStatus = "Assigned" });

            var startTicks = Stopwatch.GetTimestamp();
            var response = await client.SendAsync(request);
            var elapsed = ElapsedMilliseconds(startTicks);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            latenciesMs.Add(elapsed);
        }

        totalStopwatch.Stop();
        var metrics = BuildMetrics(latenciesMs, totalStopwatch.Elapsed.TotalSeconds);

        Console.WriteLine($"LIFECYCLE_WRITE: samples={LifecycleSampleCount}, p50={metrics.P50Ms:F2}ms, p95={metrics.P95Ms:F2}ms, max={metrics.MaxMs:F2}ms, throughput={metrics.ThroughputOpsPerSec:F2} ops/s");

        Assert.True(metrics.P95Ms <= 160, $"Lifecycle write p95 exceeded SLO: {metrics.P95Ms:F2}ms > 160ms");
        Assert.True(metrics.ThroughputOpsPerSec >= 30, $"Lifecycle write throughput below SLO: {metrics.ThroughputOpsPerSec:F2} ops/s < 30 ops/s");
    }

    [Fact]
    public async Task RealtimeBroadcast_FanoutBudget_MeetsSlo()
    {
        var tenantId = Guid.NewGuid();
        var requestStore = new InMemoryServiceRequestStore();
        var jobStore = new InMemoryJobStore();
        var userStore = new InMemoryUserStore();

        var requestIds = new List<Guid>(RealtimeSampleCount + 4);
        for (var i = 0; i < RealtimeSampleCount + 4; i++)
        {
            var requestId = Guid.NewGuid();
            requestIds.Add(requestId);
            requestStore.Seed(new ServiceRequest(requestId, tenantId, Guid.NewGuid(), $"Realtime benchmark request {i}"));
        }

        await using var app = await BuildTestApplicationAsync(requestStore, jobStore, userStore, enableRealtimePublisher: true);
        using var client = app.GetTestClient();

        var channels = new List<ChannelReader<OperationalUpdateEnvelope>>(RealtimeSubscriberCount);
        var connections = new List<HubConnection>(RealtimeSubscriberCount);
        try
        {
            for (var i = 0; i < RealtimeSubscriberCount; i++)
            {
                var connection = CreateConnection(app, tenantId, Guid.NewGuid(), "Support");
                var channel = Channel.CreateUnbounded<OperationalUpdateEnvelope>();
                connection.On<OperationalUpdateEnvelope>(
                    SignalROperationalUpdatePublisher.OperationalUpdateReceivedMethod,
                    envelope => channel.Writer.TryWrite(envelope));

                await connection.StartAsync();
                await connection.InvokeAsync<string>("SubscribeToTenantChannelAsync", tenantId.ToString());

                connections.Add(connection);
                channels.Add(channel.Reader);
            }

            var latencyMs = new List<double>(RealtimeSampleCount);
            var failures = 0;

            for (var i = 0; i < RealtimeSampleCount; i++)
            {
                var requestId = requestIds[i + 2];
                using var request = CreateAuthenticatedRequest(
                    HttpMethod.Patch,
                    $"/api/v1/requests/{requestId}/status",
                    "Customer",
                    tenantId,
                    Guid.NewGuid(),
                    new TransitionServiceRequestStatusRequest { NextStatus = "Assigned" });

                var startTicks = Stopwatch.GetTimestamp();
                var response = await client.SendAsync(request);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                var allReceived = true;
                foreach (var reader in channels)
                {
                    var received = await WaitForRequestEventAsync(reader, requestId, TimeSpan.FromSeconds(2));
                    if (!received)
                    {
                        allReceived = false;
                        break;
                    }
                }

                if (!allReceived)
                {
                    failures++;
                    continue;
                }

                latencyMs.Add(ElapsedMilliseconds(startTicks));
            }

            var metrics = BuildMetrics(latencyMs, latencyMs.Sum() / 1000d);
            var successRatio = RealtimeSampleCount == 0
                ? 0
                : (double)(RealtimeSampleCount - failures) / RealtimeSampleCount;

            Console.WriteLine($"REALTIME_FANOUT: samples={RealtimeSampleCount}, subscribers={RealtimeSubscriberCount}, successRatio={successRatio:P2}, p50={metrics.P50Ms:F2}ms, p95={metrics.P95Ms:F2}ms, max={metrics.MaxMs:F2}ms");

            Assert.True(successRatio >= 0.99, $"Realtime fanout success ratio below SLO: {successRatio:P2} < 99%");
            Assert.True(metrics.P95Ms <= 500, $"Realtime fanout p95 exceeded SLO: {metrics.P95Ms:F2}ms > 500ms");
        }
        finally
        {
            foreach (var connection in connections)
            {
                await connection.DisposeAsync();
            }
        }
    }

    private static async Task<bool> WaitForRequestEventAsync(
        ChannelReader<OperationalUpdateEnvelope> reader,
        Guid requestId,
        TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);

        try
        {
            while (true)
            {
                var envelope = await reader.ReadAsync(cancellation.Token);
                if (string.Equals(envelope.ServiceRequestStatusUpdated?.RequestId, requestId.ToString(), StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private static async Task<WebApplication> BuildTestApplicationAsync(
        InMemoryServiceRequestStore requestStore,
        InMemoryJobStore jobStore,
        InMemoryUserStore userStore,
        bool enableRealtimePublisher)
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
        builder.Services.AddSingleton<ILocalAuthService, StubLocalAuthService>();
        builder.Services.Configure<TenantResolutionOptions>(_ => { });

        if (enableRealtimePublisher)
        {
            builder.Services.AddSignalR();
            builder.Services.AddScoped<IOperationalUpdatePublisher, SignalROperationalUpdatePublisher>();
        }

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

        if (enableRealtimePublisher)
        {
            app.MapHub<OperationsHub>("/hubs/pipeline")
                .RequireAuthorization(AuthorizationPolicyCatalog.RealTimeOperations);
        }

        await app.StartAsync();
        return app;
    }

    private static HubConnection CreateConnection(WebApplication app, Guid tenantId, Guid userId, string role)
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri("http://localhost/hubs/pipeline"), options =>
            {
                options.HttpMessageHandlerFactory = _ => app.GetTestServer().CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                options.Headers.Add("Authorization", $"{TestAuthHandler.SchemeName} ok");
                options.Headers.Add(TestAuthHeaders.Subject, userId.ToString());
                options.Headers.Add(TestAuthHeaders.TenantId, tenantId.ToString());
                options.Headers.Add(TestAuthHeaders.Role, role);
                options.Headers.Add(TestAuthHeaders.TokenVersion, "1");
            })
            .Build();
    }

    private static HttpRequestMessage CreateAuthenticatedRequest(
        HttpMethod method,
        string route,
        string role,
        Guid tenantId,
        Guid userId,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, route);
        request.Headers.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName, "ok");
        request.Headers.Add(TestAuthHeaders.Subject, userId.ToString());
        request.Headers.Add(TestAuthHeaders.TenantId, tenantId.ToString());
        request.Headers.Add(TestAuthHeaders.Role, role);
        request.Headers.Add(TestAuthHeaders.TokenVersion, "1");

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    private static double ElapsedMilliseconds(long startedTimestamp)
    {
        var elapsedTicks = Stopwatch.GetTimestamp() - startedTimestamp;
        return elapsedTicks * 1000d / Stopwatch.Frequency;
    }

    private static PerformanceMetrics BuildMetrics(IReadOnlyList<double> latenciesMs, double totalSeconds)
    {
        if (latenciesMs.Count == 0)
        {
            return new PerformanceMetrics(0, 0, 0, 0);
        }

        var ordered = latenciesMs.OrderBy(x => x).ToArray();
        var p50 = Percentile(ordered, 0.50);
        var p95 = Percentile(ordered, 0.95);
        var max = ordered[^1];
        var throughput = totalSeconds <= 0 ? 0 : latenciesMs.Count / totalSeconds;
        return new PerformanceMetrics(p50, p95, max, throughput);
    }

    private static double Percentile(IReadOnlyList<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0)
        {
            return 0;
        }

        var index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
        if (index < 0)
        {
            index = 0;
        }

        if (index >= sortedValues.Count)
        {
            index = sortedValues.Count - 1;
        }

        return sortedValues[index];
    }

    private readonly record struct PerformanceMetrics(double P50Ms, double P95Ms, double MaxMs, double ThroughputOpsPerSec);

    private static class TestAuthHeaders
    {
        public const string Subject = "X-Test-Sub";
        public const string TenantId = "X-Test-Tenant-Id";
        public const string Role = "X-Test-Role";
        public const string TokenVersion = "X-Test-Ver";
    }

    private sealed class StubLocalAuthService : ILocalAuthService
    {
        public Task<LocalAuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(LocalAuthResult.Fail(401, "STUB", "stub"));

        public Task<LocalAuthResult> RegisterAsync(RegisterLocalUserRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(LocalAuthResult.Fail(401, "STUB", "stub"));
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
            var page = specification.Page ?? new PageSpecification();

            var filtered = this.items.Where(x => x.TenantId == specification.TenantId);
            if (specification.CustomerUserId.HasValue)
            {
                filtered = filtered.Where(x => x.CustomerUserId == specification.CustomerUserId.Value);
            }

            if (specification.Status.HasValue)
            {
                filtered = filtered.Where(x => x.Status == specification.Status.Value);
            }

            if (specification.AssignedWorkerUserId.HasValue)
            {
                filtered = filtered.Where(x => x.ActiveJobId.HasValue);
            }

            var result = filtered
                .Skip(page.Skip)
                .Take(page.Take)
                .ToList();

            return Task.FromResult<IReadOnlyList<ServiceRequest>>(result);
        }

        public Task<int> CountAsync(ServiceRequestQuerySpecification specification, CancellationToken cancellationToken = default)
        {
            var count = this.items.Count(x => x.TenantId == specification.TenantId);
            return Task.FromResult(count);
        }

        public void Update(ServiceRequest aggregate)
        {
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
            return Task.FromResult(this.items.Count(x => x.TenantId == specification.TenantId));
        }

        public Task<IReadOnlyDictionary<Guid, int>> GetActiveJobCountsByWorkerAsync(
            Guid tenantId,
            IReadOnlyList<Guid> workerIds,
            CancellationToken cancellationToken = default)
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
        }

        public void Remove(Job aggregate)
        {
            this.items.Remove(aggregate);
        }
    }

    private sealed class InMemoryUserStore : IUserRepository
    {
        private readonly List<User> items = new();

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
        }

        public void Remove(User aggregate)
        {
            this.items.Remove(aggregate);
        }
    }
}
