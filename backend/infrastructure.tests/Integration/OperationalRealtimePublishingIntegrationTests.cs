using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Channels;

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
using GTEK.FSM.Shared.Contracts.Api.Contracts.Realtime;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Responses;
using GTEK.FSM.Shared.Contracts.Results;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Integration;

public class OperationalRealtimePublishingIntegrationTests
{
    [Fact]
    public async Task LifecycleTransition_PublishesRealtimeEnvelope_ToRequestChannel()
    {
        var tenantId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        var requestStore = new InMemoryServiceRequestStore();
        var jobStore = new InMemoryJobStore();
        var userStore = new InMemoryUserStore();
        requestStore.Seed(new ServiceRequest(requestId, tenantId, Guid.NewGuid(), "Water leak in lobby"));

        var app = await BuildTestApplicationAsync(requestStore, jobStore, userStore);
        using var client = app.GetTestClient();
        await using var connection = CreateConnection(app, tenantId, Guid.NewGuid(), "Customer");

        var messages = Channel.CreateUnbounded<OperationalUpdateEnvelope>();
        connection.On<OperationalUpdateEnvelope>(
            SignalROperationalUpdatePublisher.OperationalUpdateReceivedMethod,
            envelope => messages.Writer.TryWrite(envelope));

        await connection.StartAsync();
        await connection.InvokeAsync<string>("SubscribeToRequestChannelAsync", requestId.ToString());

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Patch,
            $"/api/v1/requests/{requestId}/status",
            "Customer",
            tenantId,
            Guid.NewGuid(),
            new TransitionServiceRequestStatusRequest { NextStatus = "Assigned" });

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var envelope = await ReadMessageAsync(messages.Reader);
        Assert.Equal("service_request.status_updated", envelope.EventType);
        Assert.NotNull(envelope.ServiceRequestStatusUpdated);
        Assert.Equal(requestId.ToString(), envelope.ServiceRequestStatusUpdated!.RequestId);
        Assert.Equal("New", envelope.ServiceRequestStatusUpdated.PreviousStatus);
        Assert.Equal("Assigned", envelope.ServiceRequestStatusUpdated.CurrentStatus);
    }

    [Fact]
    public async Task Assignment_PublishesRealtimeEnvelope_ToJobChannel()
    {
        var tenantId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var firstWorkerId = Guid.NewGuid();
        var secondWorkerId = Guid.NewGuid();

        var requestStore = new InMemoryServiceRequestStore();
        var jobStore = new InMemoryJobStore();
        var userStore = new InMemoryUserStore();

        requestStore.Seed(new ServiceRequest(requestId, tenantId, Guid.NewGuid(), "Elevator alarm issue"));
        userStore.Seed(new User(firstWorkerId, tenantId, "wrk-01", "Worker One"));
        userStore.Seed(new User(secondWorkerId, tenantId, "wrk-02", "Worker Two"));

        var app = await BuildTestApplicationAsync(requestStore, jobStore, userStore);
        using var client = app.GetTestClient();
        await using var connection = CreateConnection(app, tenantId, Guid.NewGuid(), "Support");

        var messages = Channel.CreateUnbounded<OperationalUpdateEnvelope>();
        connection.On<OperationalUpdateEnvelope>(
            SignalROperationalUpdatePublisher.OperationalUpdateReceivedMethod,
            envelope => messages.Writer.TryWrite(envelope));

        await connection.StartAsync();

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/requests/{requestId}/assign",
            "Support",
            tenantId,
            Guid.NewGuid(),
            new AssignServiceRequestRequest { WorkerUserId = firstWorkerId.ToString() });

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var apiEnvelope = await response.Content.ReadFromJsonAsync<ApiResponse<ServiceRequestAssignmentResponse>>();
        Assert.NotNull(apiEnvelope);
        Assert.NotNull(apiEnvelope!.Data);

        var assignEnvelope = await ReadMessageAsync(messages.Reader);
        Assert.Equal("job.assignment_updated", assignEnvelope.EventType);
        Assert.NotNull(assignEnvelope.JobAssignmentUpdated);
        Assert.Equal(requestId.ToString(), assignEnvelope.JobAssignmentUpdated!.RequestId);
        Assert.Equal(firstWorkerId.ToString(), assignEnvelope.JobAssignmentUpdated.CurrentWorkerUserId);

        await connection.InvokeAsync<string>("SubscribeToJobChannelAsync", apiEnvelope.Data!.JobId);

        using var reassignRequest = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/v1/requests/{requestId}/reassign",
            "Support",
            tenantId,
            Guid.NewGuid(),
            new ReassignServiceRequestRequest { WorkerUserId = secondWorkerId.ToString(), RowVersion = apiEnvelope.Data.RowVersion });

        var reassignResponse = await client.SendAsync(reassignRequest);
        Assert.Equal(HttpStatusCode.OK, reassignResponse.StatusCode);

        var reassignEnvelope = await ReadMessageAsync(messages.Reader);
        Assert.Equal("job.assignment_updated", reassignEnvelope.EventType);
        Assert.NotNull(reassignEnvelope.JobAssignmentUpdated);
        Assert.Equal(requestId.ToString(), reassignEnvelope.JobAssignmentUpdated!.RequestId);
        Assert.Equal(firstWorkerId.ToString(), reassignEnvelope.JobAssignmentUpdated.PreviousWorkerUserId);
        Assert.Equal(secondWorkerId.ToString(), reassignEnvelope.JobAssignmentUpdated.CurrentWorkerUserId);
    }

    [Fact]
    public async Task LifecycleTransition_DoesNotLeak_ToOtherTenantChannel()
    {
        var sourceTenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        var requestStore = new InMemoryServiceRequestStore();
        var jobStore = new InMemoryJobStore();
        var userStore = new InMemoryUserStore();
        requestStore.Seed(new ServiceRequest(requestId, sourceTenantId, Guid.NewGuid(), "Power outage in level 2"));

        var app = await BuildTestApplicationAsync(requestStore, jobStore, userStore);
        using var client = app.GetTestClient();
        await using var connection = CreateConnection(app, otherTenantId, Guid.NewGuid(), "Manager");

        var messages = Channel.CreateUnbounded<OperationalUpdateEnvelope>();
        connection.On<OperationalUpdateEnvelope>(
            SignalROperationalUpdatePublisher.OperationalUpdateReceivedMethod,
            envelope => messages.Writer.TryWrite(envelope));

        await connection.StartAsync();

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Patch,
            $"/api/v1/requests/{requestId}/status",
            "Customer",
            sourceTenantId,
            Guid.NewGuid(),
            new TransitionServiceRequestStatusRequest { NextStatus = "Assigned" });

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var leaked = await TryReadMessageAsync(messages.Reader, TimeSpan.FromMilliseconds(500));
        Assert.Null(leaked);
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
        builder.Services.AddScoped<IOperationalUpdatePublisher, SignalROperationalUpdatePublisher>();
        builder.Services.Configure<TenantResolutionOptions>(_ => { });
        builder.Services.AddSignalR();

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
        app.MapHub<OperationsHub>("/hubs/pipeline")
            .RequireAuthorization(AuthorizationPolicyCatalog.RealTimeOperations);

        await app.StartAsync();
        return app;
    }

    private static async Task<OperationalUpdateEnvelope> ReadMessageAsync(ChannelReader<OperationalUpdateEnvelope> reader)
    {
        var envelope = await TryReadMessageAsync(reader, TimeSpan.FromSeconds(3));
        Assert.NotNull(envelope);
        return envelope!;
    }

    private static async Task<OperationalUpdateEnvelope?> TryReadMessageAsync(ChannelReader<OperationalUpdateEnvelope> reader, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);

        try
        {
            return await reader.ReadAsync(cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            return null;
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
            return Task.FromResult(this.items.Count(x => x.TenantId == specification.TenantId));
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
        }

        public void Remove(User aggregate)
        {
            this.items.Remove(aggregate);
        }
    }
}