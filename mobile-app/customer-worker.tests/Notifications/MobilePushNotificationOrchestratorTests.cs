namespace GTEK.FSM.MobileApp.Tests.Notifications;

using GTEK.FSM.MobileApp.Services.Notifications;
using GTEK.FSM.MobileApp.Services.Realtime;
using GTEK.FSM.MobileApp.State;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Realtime;

public sealed class MobilePushNotificationOrchestratorTests
{
    [Fact]
    public async Task StartAsync_PublishesStatusNotification_ForMatchingCustomerTenant()
    {
        var realtimeClient = new StubRealtimeClient();
        var inbox = new MobileNotificationInboxState();
        var session = new SessionContextState();
        session.Update("customer-1", "Customer", true);
        var tenant = new TenantContextState();
        tenant.Update("tenant-a", "Tenant A");
        var navigator = new RecordingNavigator();
        var publisher = new InAppLocalNotificationPublisher(new NoOpDiagnosticsLogger(), inbox);
        var sut = new MobilePushNotificationOrchestrator(realtimeClient, publisher, navigator, session, tenant);

        await sut.StartAsync();
        await realtimeClient.PublishStatusAsync(new ServiceRequestStatusUpdatedEvent
        {
            RequestId = "REQ-100",
            TenantId = "tenant-a",
            CurrentStatus = "InProgress",
            UpdatedAtUtc = new DateTime(2026, 4, 2, 14, 0, 0, DateTimeKind.Utc),
        });

        var notification = Assert.Single(inbox.Notifications);
        Assert.Equal("service_request.status_updated", notification.NotificationType);
        Assert.Contains("CustomerRequests", notification.Route);
    }

    [Fact]
    public async Task StartAsync_DoesNotDuplicateSubscriptions_WhenCalledTwice()
    {
        var realtimeClient = new StubRealtimeClient();
        var session = new SessionContextState();
        session.Update("customer-1", "Customer", true);
        var tenant = new TenantContextState();
        tenant.Update("tenant-a", "Tenant A");
        var sut = new MobilePushNotificationOrchestrator(
            realtimeClient,
            new InAppLocalNotificationPublisher(new NoOpDiagnosticsLogger(), new MobileNotificationInboxState()),
            new RecordingNavigator(),
            session,
            tenant);

        await sut.StartAsync();
        await sut.StartAsync();

        Assert.Equal(1, realtimeClient.StatusSubscriptionCount);
        Assert.Equal(1, realtimeClient.AssignmentSubscriptionCount);
    }

    [Fact]
    public async Task HandleNotificationTapAsync_IgnoresTenantMismatch()
    {
        var realtimeClient = new StubRealtimeClient();
        var session = new SessionContextState();
        session.Update("worker-1", "Worker", true);
        var tenant = new TenantContextState();
        tenant.Update("tenant-a", "Tenant A");
        var navigator = new RecordingNavigator();
        var sut = new MobilePushNotificationOrchestrator(
            realtimeClient,
            new InAppLocalNotificationPublisher(new NoOpDiagnosticsLogger(), new MobileNotificationInboxState()),
            navigator,
            session,
            tenant);

        await sut.HandleNotificationTapAsync(new MobileNotificationPayload(
            "job.assignment_updated",
            "Assignment Updated",
            "Job JOB-1 is now Assigned.",
            "WorkerJobs?jobId=JOB-1",
            "tenant-b",
            DateTime.UtcNow));

        Assert.Null(navigator.LastPayload);
    }

    private sealed class StubRealtimeClient : IMobileOperationalRealtimeClient
    {
        private Func<ServiceRequestStatusUpdatedEvent, Task>? _statusHandler;
        private Func<JobAssignmentUpdatedEvent, Task>? _assignmentHandler;

        public MobileOperationalRealtimeConnectionState ConnectionState => MobileOperationalRealtimeConnectionState.Connected;

        public event Action<MobileOperationalRealtimeConnectionState>? ConnectionStateChanged
        {
            add { }
            remove { }
        }

        public int StatusSubscriptionCount { get; private set; }

        public int AssignmentSubscriptionCount { get; private set; }

        public IDisposable SubscribeToStatusUpdates(Func<ServiceRequestStatusUpdatedEvent, Task> handler)
        {
            StatusSubscriptionCount++;
            _statusHandler = handler;
            return new CallbackDisposable(() => _statusHandler = null);
        }

        public IDisposable SubscribeToAssignmentUpdates(Func<JobAssignmentUpdatedEvent, Task> handler)
        {
            AssignmentSubscriptionCount++;
            _assignmentHandler = handler;
            return new CallbackDisposable(() => _assignmentHandler = null);
        }

        public Task EnsureConnectedAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task PublishStatusAsync(ServiceRequestStatusUpdatedEvent payload)
        {
            return _statusHandler is null ? Task.CompletedTask : _statusHandler(payload);
        }
    }

    private sealed class RecordingNavigator : IMobileNotificationDeepLinkNavigator
    {
        public MobileNotificationPayload? LastPayload { get; private set; }

        public Task NavigateAsync(MobileNotificationPayload payload, CancellationToken cancellationToken = default)
        {
            LastPayload = payload;
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpDiagnosticsLogger : GTEK.FSM.MobileApp.Services.Diagnostics.IMobileDiagnosticsLogger
    {
        public void Error(string category, string message)
        {
        }

        public void Info(string category, string message)
        {
        }

        public void Warn(string category, string message)
        {
        }
    }

    private sealed class CallbackDisposable : IDisposable
    {
        private readonly Action _onDispose;

        public CallbackDisposable(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose();
        }
    }
}