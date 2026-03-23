namespace GTEK.FSM.MobileApp.State;

public enum MobileFetchState
{
    Idle,
    Loading,
    Success,
    Stale,
    Retrying,
    Partial,
    Failed,
}

public sealed class ConnectivityRecoveryState
{
    public MobileFetchState CurrentState { get; private set; } = MobileFetchState.Idle;

    public string Message { get; private set; } = string.Empty;

    public int RetryAttempts { get; private set; }

    public int MaxRetries { get; private set; }

    public DateTimeOffset? LastSuccessUtc { get; private set; }

    public bool HasFreshData => LastSuccessUtc.HasValue;

    public void SetLoading()
    {
        CurrentState = MobileFetchState.Loading;
        Message = "Loading connectivity checks...";
        RetryAttempts = 0;
    }

    public void SetRetrying(int retryAttempt, int maxRetries, string message)
    {
        CurrentState = MobileFetchState.Retrying;
        RetryAttempts = retryAttempt;
        MaxRetries = maxRetries;
        Message = message;
    }

    public void SetSuccess(string message)
    {
        CurrentState = MobileFetchState.Success;
        Message = message;
        RetryAttempts = 0;
        LastSuccessUtc = DateTimeOffset.UtcNow;
    }

    public void SetStale(string message)
    {
        CurrentState = MobileFetchState.Stale;
        Message = message;
    }

    public void SetPartial(string message)
    {
        CurrentState = MobileFetchState.Partial;
        Message = message;
    }

    public void SetFailed(string message)
    {
        CurrentState = MobileFetchState.Failed;
        Message = message;
    }
}
