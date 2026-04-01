using GTEK.FSM.Shared.Contracts.Api.Contracts.Realtime;

namespace GTEK.FSM.MobileApp.Services.Realtime;

public interface IMobileOperationalRealtimeClient
{
    MobileOperationalRealtimeConnectionState ConnectionState { get; }

    event Action<MobileOperationalRealtimeConnectionState>? ConnectionStateChanged;

    IDisposable SubscribeToStatusUpdates(Func<ServiceRequestStatusUpdatedEvent, Task> handler);

    IDisposable SubscribeToAssignmentUpdates(Func<JobAssignmentUpdatedEvent, Task> handler);

    Task EnsureConnectedAsync(CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);
}