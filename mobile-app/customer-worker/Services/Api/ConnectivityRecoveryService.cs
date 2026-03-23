namespace GTEK.FSM.MobileApp.Services.Api;

using GTEK.FSM.MobileApp.State;

public interface IConnectivityRecoveryService
{
    Task EvaluateStartupConnectivityAsync(CancellationToken cancellationToken = default);
}

public sealed class ConnectivityRecoveryService : IConnectivityRecoveryService
{
    private const int MaxRetries = 2;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(350);

    private readonly ConnectivityRecoveryState _state;
    private readonly IAuthenticatedApiProbeService _authenticatedProbe;
    private readonly ITenantOwnershipProbeService _tenantProbe;

    public ConnectivityRecoveryService(
        ConnectivityRecoveryState state,
        IAuthenticatedApiProbeService authenticatedProbe,
        ITenantOwnershipProbeService tenantProbe)
    {
        _state = state;
        _authenticatedProbe = authenticatedProbe;
        _tenantProbe = tenantProbe;
    }

    public async Task EvaluateStartupConnectivityAsync(CancellationToken cancellationToken = default)
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

            return;
        }

        var tenantOk = await ExecuteWithRetryAsync(
            operationName: "Tenant ownership probe",
            operation: () => _tenantProbe.ProbeReadBoundaryAsync(cancellationToken),
            cancellationToken);

        if (tenantOk)
        {
            _state.SetSuccess("Authenticated and tenant boundary checks passed.");
            return;
        }

        if (_state.HasFreshData)
        {
            _state.SetStale("Tenant boundary probe failed; using stale cached context.");
            return;
        }

        _state.SetPartial("Authenticated, but tenant boundary probe failed.");
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
}
