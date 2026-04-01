using GTEK.FSM.MobileApp.Configuration;
using GTEK.FSM.MobileApp.Services.Identity;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Realtime;

using Microsoft.AspNetCore.SignalR.Client;

namespace GTEK.FSM.MobileApp.Services.Realtime;

public sealed class SignalRMobileOperationalRealtimeClient : IMobileOperationalRealtimeClient, IAsyncDisposable
{
    private const string OperationalUpdateReceivedMethod = "OperationalUpdateReceived";

    private readonly ApiEndpointConfiguration endpointConfiguration;
    private readonly IIdentityTokenProvider tokenProvider;
    private readonly SemaphoreSlim connectionLock = new(1, 1);
    private readonly List<Func<ServiceRequestStatusUpdatedEvent, Task>> statusHandlers = [];
    private readonly List<Func<JobAssignmentUpdatedEvent, Task>> assignmentHandlers = [];

    private HubConnection? connection;

    public SignalRMobileOperationalRealtimeClient(
        ApiEndpointConfiguration endpointConfiguration,
        IIdentityTokenProvider tokenProvider)
    {
        this.endpointConfiguration = endpointConfiguration;
        this.tokenProvider = tokenProvider;
        this.ConnectionState = MobileOperationalRealtimeConnectionState.Disconnected;
    }

    public MobileOperationalRealtimeConnectionState ConnectionState { get; private set; }

    public event Action<MobileOperationalRealtimeConnectionState>? ConnectionStateChanged;

    public IDisposable SubscribeToStatusUpdates(Func<ServiceRequestStatusUpdatedEvent, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        this.statusHandlers.Add(handler);
        return new Subscription(() => this.statusHandlers.Remove(handler));
    }

    public IDisposable SubscribeToAssignmentUpdates(Func<JobAssignmentUpdatedEvent, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        this.assignmentHandlers.Add(handler);
        return new Subscription(() => this.assignmentHandlers.Remove(handler));
    }

    public async Task EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        await this.connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (this.connection?.State == HubConnectionState.Connected)
            {
                this.UpdateConnectionState(MobileOperationalRealtimeConnectionState.Connected);
                return;
            }

            var accessToken = this.tokenProvider.GetAccessToken();
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                this.UpdateConnectionState(MobileOperationalRealtimeConnectionState.AuthenticationRequired);
                return;
            }

            this.connection ??= this.BuildConnection(accessToken);
            this.UpdateConnectionState(MobileOperationalRealtimeConnectionState.Connecting);
            await this.connection.StartAsync(cancellationToken);
            this.UpdateConnectionState(MobileOperationalRealtimeConnectionState.Connected);
        }
        catch
        {
            this.UpdateConnectionState(MobileOperationalRealtimeConnectionState.Faulted);
            throw;
        }
        finally
        {
            this.connectionLock.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await this.connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (this.connection is null)
            {
                this.UpdateConnectionState(MobileOperationalRealtimeConnectionState.Disconnected);
                return;
            }

            await this.connection.StopAsync(cancellationToken);
            await this.connection.DisposeAsync();
            this.connection = null;
            this.UpdateConnectionState(MobileOperationalRealtimeConnectionState.Disconnected);
        }
        finally
        {
            this.connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await this.DisconnectAsync();
        this.connectionLock.Dispose();
    }

    private HubConnection BuildConnection(string accessToken)
    {
        var hubUri = this.ResolveHubUri();
        var builtConnection = new HubConnectionBuilder()
            .WithUrl(hubUri, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
            })
            .WithAutomaticReconnect([TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)])
            .Build();

        builtConnection.On<OperationalUpdateEnvelope>(OperationalUpdateReceivedMethod, this.HandleOperationalUpdateAsync);
        builtConnection.Reconnecting += error =>
        {
            this.UpdateConnectionState(MobileOperationalRealtimeConnectionState.Reconnecting);
            return Task.CompletedTask;
        };
        builtConnection.Reconnected += connectionId =>
        {
            this.UpdateConnectionState(MobileOperationalRealtimeConnectionState.Connected);
            return Task.CompletedTask;
        };
        builtConnection.Closed += error =>
        {
            this.UpdateConnectionState(MobileOperationalRealtimeConnectionState.Disconnected);
            return Task.CompletedTask;
        };

        return builtConnection;
    }

    private Uri ResolveHubUri()
    {
        return new Uri($"{this.endpointConfiguration.ApiBaseUrl.TrimEnd('/')}/hubs/pipeline", UriKind.Absolute);
    }

    private async Task HandleOperationalUpdateAsync(OperationalUpdateEnvelope envelope)
    {
        if (envelope.ServiceRequestStatusUpdated is not null)
        {
            foreach (var handler in this.statusHandlers.ToArray())
            {
                await handler(envelope.ServiceRequestStatusUpdated);
            }
        }

        if (envelope.JobAssignmentUpdated is not null)
        {
            foreach (var handler in this.assignmentHandlers.ToArray())
            {
                await handler(envelope.JobAssignmentUpdated);
            }
        }
    }

    private void UpdateConnectionState(MobileOperationalRealtimeConnectionState newState)
    {
        if (this.ConnectionState == newState)
        {
            return;
        }

        this.ConnectionState = newState;
        this.ConnectionStateChanged?.Invoke(newState);
    }

    private sealed class Subscription(Action dispose) : IDisposable
    {
        private Action? dispose = dispose;

        public void Dispose()
        {
            Interlocked.Exchange(ref this.dispose, null)?.Invoke();
        }
    }
}