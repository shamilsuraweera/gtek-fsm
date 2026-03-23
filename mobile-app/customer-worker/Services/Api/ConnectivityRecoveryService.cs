namespace GTEK.FSM.MobileApp.Services.Api;

using System.Collections.Concurrent;
using GTEK.FSM.MobileApp.Services.Diagnostics;
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
    private readonly IMobileDiagnosticsLogger _diagnostics;
    private readonly ConcurrentQueue<string> _backgroundQueue = new();
    private readonly SemaphoreSlim _flushGate = new(1, 1);

    public ConnectivityRecoveryService(
        ConnectivityRecoveryState state,
        IAuthenticatedApiProbeService authenticatedProbe,
        ITenantOwnershipProbeService tenantProbe,
        IMobileDiagnosticsLogger diagnostics)
    {
        _state = state;
        _authenticatedProbe = authenticatedProbe;
        _tenantProbe = tenantProbe;
        _diagnostics = diagnostics;

        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    public async Task EvaluateStartupConnectivityAsync(CancellationToken cancellationToken = default)
    {
        if (!HasInternetAccess())
        {
            _state.SetStale("Network unavailable; queued connectivity checks for background sync.");
            _diagnostics.Warn("connectivity", "Internet unavailable during startup check; queued connectivity probe.");
            await QueueBackgroundSyncAsync(cancellationToken);
            return;
        }

        _state.SetLoading();

        var isHealthy = await EvaluateConnectivityCoreAsync(cancellationToken);
        if (!isHealthy)
        {
            _diagnostics.Warn("connectivity", "Startup connectivity check degraded; queueing background sync work item.");
            await QueueBackgroundSyncAsync(cancellationToken);
        }
        else
        {
            _diagnostics.Info("connectivity", "Startup connectivity checks completed successfully.");
        }
    }

    public Task QueueBackgroundSyncAsync(CancellationToken cancellationToken = default)
    {
        _backgroundQueue.Enqueue("startup-connectivity-probe");
        _diagnostics.Info("connectivity.queue", $"Queued background connectivity work item. Queue depth={_backgroundQueue.Count}.");

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

            _diagnostics.Warn("connectivity", "Authentication probe path failed during connectivity evaluation.");
            return false;
        }

        var tenantOk = await ExecuteWithRetryAsync(
            operationName: "Tenant ownership probe",
            operation: () => _tenantProbe.ProbeReadBoundaryAsync(cancellationToken),
            cancellationToken);

        if (tenantOk)
        {
            _state.SetSuccess("Authenticated and tenant boundary checks passed.");
            _diagnostics.Info("connectivity", "Connectivity evaluation succeeded for auth and tenant checks.");
            return true;
        }

        if (_state.HasFreshData)
        {
            _state.SetStale("Tenant boundary probe failed; using stale cached context.");
            _diagnostics.Warn("connectivity", "Tenant probe failed; stale cached context retained.");
            return false;
        }

        _state.SetPartial("Authenticated, but tenant boundary probe failed.");
        _diagnostics.Warn("connectivity", "Tenant probe failed after auth success; marked as partial failure.");
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

                _diagnostics.Warn("connectivity.retry", $"{operationName} attempt {attempt + 1} returned unsuccessful status.");
            }
            catch
            {
                _diagnostics.Error("connectivity.retry", $"{operationName} attempt {attempt + 1} threw an exception.");
            }
        }

        return false;
    }

    private async void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
    {
        if (e.NetworkAccess == NetworkAccess.Internet)
        {
            _diagnostics.Info("connectivity", "Internet restored; attempting to flush queued background work.");
            await FlushQueueAsync();
        }
        else
        {
            _diagnostics.Warn("connectivity", $"Connectivity changed to {e.NetworkAccess}; queue will hold work until internet is available.");
        }
    }

    private async Task FlushQueueAsync(CancellationToken cancellationToken = default)
    {
        if (!HasInternetAccess())
        {
            _diagnostics.Warn("connectivity.queue", "Flush skipped because internet access is not available.");
            return;
        }

        if (!await _flushGate.WaitAsync(0, cancellationToken))
        {
            _diagnostics.Info("connectivity.queue", "Flush already in progress; skipping concurrent flush attempt.");
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
                    _diagnostics.Warn("connectivity.queue", "Internet dropped during queue flush; work item re-queued.");
                    return;
                }

                _diagnostics.Info("connectivity.queue", "Processing queued connectivity work item.");
                await EvaluateConnectivityCoreAsync(cancellationToken);
            }

            _diagnostics.Info("connectivity.queue", "Queue flush completed.");
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
