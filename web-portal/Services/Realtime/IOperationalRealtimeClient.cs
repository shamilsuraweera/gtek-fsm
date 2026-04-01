using GTEK.FSM.Shared.Contracts.Api.Contracts.Realtime;

namespace GTEK.FSM.WebPortal.Services.Realtime;

public interface IOperationalRealtimeClient
{
    bool IsEnabled { get; }

    OperationalRealtimeConnectionState ConnectionState { get; }

    event Action<OperationalRealtimeConnectionState>? ConnectionStateChanged;

    IDisposable SubscribeToStatusUpdates(Func<ServiceRequestStatusUpdatedEvent, Task> handler);

    IDisposable SubscribeToAssignmentUpdates(Func<JobAssignmentUpdatedEvent, Task> handler);

    Task EnsureConnectedAsync(CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);
}