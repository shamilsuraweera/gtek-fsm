using GTEK.FSM.Shared.Contracts.Api.Contracts.Realtime;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

namespace GTEK.FSM.WebPortal.Services.Realtime;

public sealed class SignalROperationalRealtimeClient : IOperationalRealtimeClient, IAsyncDisposable
{
    private const string OperationalUpdateReceivedMethod = "OperationalUpdateReceived";

    private readonly NavigationManager navigationManager;
    private readonly PortalRealtimeOptions options;
    private readonly IPortalAccessTokenProvider accessTokenProvider;
    private readonly SemaphoreSlim connectionLock = new(1, 1);
    private readonly List<Func<ServiceRequestStatusUpdatedEvent, Task>> statusHandlers = [];
    private readonly List<Func<JobAssignmentUpdatedEvent, Task>> assignmentHandlers = [];

    private HubConnection? connection;

    public SignalROperationalRealtimeClient(
        NavigationManager navigationManager,
        IOptions<PortalRealtimeOptions> options,
        IPortalAccessTokenProvider accessTokenProvider)
    {
        this.navigationManager = navigationManager;
        this.options = options.Value;
        this.accessTokenProvider = accessTokenProvider;
        this.ConnectionState = this.options.Enabled
            ? OperationalRealtimeConnectionState.Disconnected
            : OperationalRealtimeConnectionState.Disabled;
    }

    public bool IsEnabled => this.options.Enabled;

    public OperationalRealtimeConnectionState ConnectionState { get; private set; }

    public event Action<OperationalRealtimeConnectionState>? ConnectionStateChanged;

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
        if (!this.IsEnabled)
        {
            this.UpdateConnectionState(OperationalRealtimeConnectionState.Disabled);
            return;
        }

        await this.connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (this.connection?.State == HubConnectionState.Connected)
            {
                this.UpdateConnectionState(OperationalRealtimeConnectionState.Connected);
                return;
            }

            var accessToken = await this.accessTokenProvider.GetAccessTokenAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                this.UpdateConnectionState(OperationalRealtimeConnectionState.AuthenticationRequired);
                return;
            }

            this.connection ??= this.BuildConnection(accessToken);
            this.UpdateConnectionState(OperationalRealtimeConnectionState.Connecting);
            await this.connection.StartAsync(cancellationToken);
            this.UpdateConnectionState(OperationalRealtimeConnectionState.Connected);
        }
        catch
        {
            this.UpdateConnectionState(OperationalRealtimeConnectionState.Faulted);
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
                this.UpdateConnectionState(this.IsEnabled ? OperationalRealtimeConnectionState.Disconnected : OperationalRealtimeConnectionState.Disabled);
                return;
            }

            await this.connection.StopAsync(cancellationToken);
            await this.connection.DisposeAsync();
            this.connection = null;
            this.UpdateConnectionState(this.IsEnabled ? OperationalRealtimeConnectionState.Disconnected : OperationalRealtimeConnectionState.Disabled);
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
            this.UpdateConnectionState(OperationalRealtimeConnectionState.Reconnecting);
            return Task.CompletedTask;
        };
        builtConnection.Reconnected += connectionId =>
        {
            this.UpdateConnectionState(OperationalRealtimeConnectionState.Connected);
            return Task.CompletedTask;
        };
        builtConnection.Closed += error =>
        {
            this.UpdateConnectionState(this.IsEnabled ? OperationalRealtimeConnectionState.Disconnected : OperationalRealtimeConnectionState.Disabled);
            return Task.CompletedTask;
        };

        return builtConnection;
    }

    private Uri ResolveHubUri()
    {
        var baseUri = string.IsNullOrWhiteSpace(this.options.BaseUrl)
            ? new Uri(this.navigationManager.BaseUri, UriKind.Absolute)
            : new Uri(this.options.BaseUrl, UriKind.Absolute);

        return new Uri(baseUri, this.options.HubPath);
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

    private void UpdateConnectionState(OperationalRealtimeConnectionState newState)
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