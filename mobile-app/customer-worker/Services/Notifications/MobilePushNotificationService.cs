namespace GTEK.FSM.MobileApp.Services.Notifications;

using GTEK.FSM.MobileApp.Services.Diagnostics;
using GTEK.FSM.MobileApp.Navigation;
using GTEK.FSM.MobileApp.Services.Realtime;
using GTEK.FSM.MobileApp.State;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Realtime;

public sealed record MobileNotificationPayload(
    string NotificationType,
    string Title,
    string Message,
    string Route,
    string TenantId,
    DateTime IssuedAtUtc);

public interface ILocalNotificationPublisher
{
    Task PublishAsync(MobileNotificationPayload payload, CancellationToken cancellationToken = default);
}

public interface IMobileNotificationDeepLinkNavigator
{
    Task NavigateAsync(MobileNotificationPayload payload, CancellationToken cancellationToken = default);
}

public interface IMobilePushNotificationOrchestrator
{
    Task StartAsync(CancellationToken cancellationToken = default);

    void Stop();

    Task HandleNotificationTapAsync(MobileNotificationPayload payload, CancellationToken cancellationToken = default);
}

public static class MobileNotificationPayloadFactory
{
    public static bool TryBuildForStatusUpdate(
        ServiceRequestStatusUpdatedEvent payload,
        string role,
        string tenantId,
        out MobileNotificationPayload notification)
    {
        notification = default!;

        if (!IsTenantMatch(payload.TenantId, tenantId) || !RoleGateResolver.ContainsRole(role, "customer"))
        {
            return false;
        }

        var route = $"CustomerRequests?requestId={Uri.EscapeDataString(payload.RequestId)}";
        notification = new MobileNotificationPayload(
            NotificationType: "service_request.status_updated",
            Title: "Request Status Updated",
            Message: $"Request {payload.RequestId} moved to {payload.CurrentStatus}.",
            Route: route,
            TenantId: payload.TenantId,
            IssuedAtUtc: payload.UpdatedAtUtc);

        return true;
    }

    public static bool TryBuildForAssignmentUpdate(
        JobAssignmentUpdatedEvent payload,
        string role,
        string tenantId,
        out MobileNotificationPayload notification)
    {
        notification = default!;

        if (!IsTenantMatch(payload.TenantId, tenantId) || !RoleGateResolver.ContainsRole(role, "worker"))
        {
            return false;
        }

        var route = $"WorkerJobs?jobId={Uri.EscapeDataString(payload.JobId)}&requestId={Uri.EscapeDataString(payload.RequestId)}";
        notification = new MobileNotificationPayload(
            NotificationType: "job.assignment_updated",
            Title: "Assignment Updated",
            Message: $"Job {payload.JobId} is now {payload.AssignmentStatus}.",
            Route: route,
            TenantId: payload.TenantId,
            IssuedAtUtc: payload.UpdatedAtUtc);

        return true;
    }

    private static bool IsTenantMatch(string eventTenantId, string currentTenantId)
    {
        return !string.IsNullOrWhiteSpace(eventTenantId)
            && !string.IsNullOrWhiteSpace(currentTenantId)
            && string.Equals(eventTenantId, currentTenantId, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class InAppLocalNotificationPublisher : ILocalNotificationPublisher
{
    private readonly IMobileDiagnosticsLogger _diagnostics;
    private readonly MobileNotificationInboxState _inbox;

    public InAppLocalNotificationPublisher(IMobileDiagnosticsLogger diagnostics, MobileNotificationInboxState inbox)
    {
        _diagnostics = diagnostics;
        _inbox = inbox;
    }

    public Task PublishAsync(MobileNotificationPayload payload, CancellationToken cancellationToken = default)
    {
        _inbox.Add(payload);
        _diagnostics.Info("notifications.local", $"Notification queued: {payload.Title} -> {payload.Route}");
        return Task.CompletedTask;
    }
}

public sealed class ShellNotificationDeepLinkNavigator : IMobileNotificationDeepLinkNavigator
{
    public Task NavigateAsync(MobileNotificationPayload payload, CancellationToken cancellationToken = default)
    {
        return MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (Shell.Current is null || string.IsNullOrWhiteSpace(payload.Route))
            {
                return;
            }

            await Shell.Current.GoToAsync(payload.Route);
        });
    }
}

public sealed class MobilePushNotificationOrchestrator : IMobilePushNotificationOrchestrator
{
    private readonly IMobileOperationalRealtimeClient _realtimeClient;
    private readonly ILocalNotificationPublisher _publisher;
    private readonly IMobileNotificationDeepLinkNavigator _navigator;
    private readonly SessionContextState _session;
    private readonly TenantContextState _tenant;

    private IDisposable _statusSubscription = NoOpDisposable.Instance;
    private IDisposable _assignmentSubscription = NoOpDisposable.Instance;

    public MobilePushNotificationOrchestrator(
        IMobileOperationalRealtimeClient realtimeClient,
        ILocalNotificationPublisher publisher,
        IMobileNotificationDeepLinkNavigator navigator,
        SessionContextState session,
        TenantContextState tenant)
    {
        _realtimeClient = realtimeClient;
        _publisher = publisher;
        _navigator = navigator;
        _session = session;
        _tenant = tenant;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (ReferenceEquals(_statusSubscription, NoOpDisposable.Instance))
        {
            _statusSubscription = _realtimeClient.SubscribeToStatusUpdates(HandleStatusUpdateAsync);
        }

        if (ReferenceEquals(_assignmentSubscription, NoOpDisposable.Instance))
        {
            _assignmentSubscription = _realtimeClient.SubscribeToAssignmentUpdates(HandleAssignmentUpdateAsync);
        }

        return Task.CompletedTask;
    }

    public void Stop()
    {
        _statusSubscription?.Dispose();
        _assignmentSubscription?.Dispose();
        _statusSubscription = NoOpDisposable.Instance;
        _assignmentSubscription = NoOpDisposable.Instance;
    }

    public Task HandleNotificationTapAsync(MobileNotificationPayload payload, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(payload.TenantId, _tenant.TenantId, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        return _navigator.NavigateAsync(payload, cancellationToken);
    }

    private async Task HandleStatusUpdateAsync(ServiceRequestStatusUpdatedEvent payload)
    {
        if (!MobileNotificationPayloadFactory.TryBuildForStatusUpdate(payload, _session.Role, _tenant.TenantId, out var notification))
        {
            return;
        }

        await _publisher.PublishAsync(notification);
    }

    private async Task HandleAssignmentUpdateAsync(JobAssignmentUpdatedEvent payload)
    {
        if (!MobileNotificationPayloadFactory.TryBuildForAssignmentUpdate(payload, _session.Role, _tenant.TenantId, out var notification))
        {
            return;
        }

        await _publisher.PublishAsync(notification);
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public static readonly NoOpDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}