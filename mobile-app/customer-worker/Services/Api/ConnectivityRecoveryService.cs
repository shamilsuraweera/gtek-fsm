namespace GTEK.FSM.MobileApp.Services.Api;

using System.Collections.Concurrent;
using GTEK.FSM.MobileApp.State;
using Microsoft.Maui.Networking;

public interface IConnectivityRecoveryService
{
    Task EvaluateStartupConnectivityAsync(CancellationToken cancellationToken = default);
    Task QueueBackgroundSyncAsync(CancellationToken cancellationToken = default);
}

public sealed class ConnectivityRecoveryService : IConnectivityRecoveryService
{
    private const int MaxRetries = 2;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(350);

    private readonly ConnectivityRecoveryState _state;
    private readonly IAuthenticatedApiProbeService _authenticatedProbe;
    private readonly ITenantOwnershipProbeService _tenantProbe;
    private readonly ConcurrentQueue<string> _backgroundQueue = new();
    private readonly SemaphoreSlim _flushGate = new(1, 1);

    public ConnectivityRecoveryService(
        ConnectivityRecoveryState state,
        IAuthenticatedApiProbeService authenticatedProbe,
        ITenantOwnershipProbeService tenantProbe)
    {
        _state = state;
        _authenticatedProbe = authenticatedProbe;
        _tenantProbe = tenantProbe;

        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    public async Task EvaluateStartupConnectivityAsync(CancellationToken cancellationToken = default)
    {
        if (!HasInternetAccess())
        {
            _state.SetStale("Network unavailable; queued connectivity checks for background sync.");
            await QueueBackgroundSyncAsync(cancellationToken);
            return;
        }

        _state.SetLoading();

        var isHealthy = await EvaluateConnectivityCoreAsync(cancellationToken);
        if (!isHealthy)
        {
            await QueueBackgroundSyncAsync(cancellationToken);
        }
    }

    public Task QueueBackgroundSyncAsync(CancellationToken cancellationToken = default)
    {
        _backgroundQueue.Enqueue("startup-connectivity-probe");

        if (HasInternetAccess())
        {
            _ = FlushQueueAsync(cancellationToken);
        }

        return Task.CompletedTask;
    }

    private async Task<bool> EvaluateConnectivityCoreAsync(CancellationToken cancellationToken)
    {
        _state.SetLoading();

        var authOk = await ExecuteWithRetryAsync(
            operationName: "Authentication probe",
            operation: () => _authenticatedProbe.ProbeAuthenticatedAsync(cancellationToken),
            cancellationToken);

        if (!authOk)
        {
            if (_state.HasFreshData)
            {
                _state.SetStale("Authentication probe failed; showing stale cached context.");
            }
            else
            {
                _state.SetFailed("Authentication probe failed; unable to establish mobile API session.");
            }

            return false;
        }

        var tenantOk = await ExecuteWithRetryAsync(
            operationName: "Tenant ownership probe",
            operation: () => _tenantProbe.ProbeReadBoundaryAsync(cancellationToken),
            cancellationToken);

        if (tenantOk)
        {
            _state.SetSuccess("Authenticated and tenant boundary checks passed.");
            return true;
        }

        if (_state.HasFreshData)
        {
            _state.SetStale("Tenant boundary probe failed; using stale cached context.");
            return false;
        }

        _state.SetPartial("Authenticated, but tenant boundary probe failed.");
        return false;
    }

    private async Task<bool> ExecuteWithRetryAsync(
        string operationName,
        Func<Task<bool>> operation,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            if (attempt > 0)
            {
                _state.SetRetrying(
                    retryAttempt: attempt,
                    maxRetries: MaxRetries,
                    message: $"{operationName} retry {attempt} of {MaxRetries} in progress...");

                await Task.Delay(RetryDelay, cancellationToken);
            }

            try
            {
                if (await operation())
                {
                    return true;
                }
            }
            catch
            {
                // Keep retry behavior deterministic for startup diagnostics.
            }
        }

        return false;
    }

    private async void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
    {
        if (e.NetworkAccess == NetworkAccess.Internet)
        {
            await FlushQueueAsync();
        }
    }

    private async Task FlushQueueAsync(CancellationToken cancellationToken = default)
    {
        if (!HasInternetAccess())
        {
            return;
        }

        if (!await _flushGate.WaitAsync(0, cancellationToken))
        {
            return;
        }

        try
        {
            while (_backgroundQueue.TryDequeue(out _))
            {
                if (!HasInternetAccess())
                {
                    _backgroundQueue.Enqueue("startup-connectivity-probe");
                    _state.SetStale("Network interrupted during sync; queued remaining work.");
                    return;
                }

                await EvaluateConnectivityCoreAsync(cancellationToken);
            }
        }
        finally
        {
            _flushGate.Release();
        }
    }

    private static bool HasInternetAccess()
    {
        return Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
    }
}
