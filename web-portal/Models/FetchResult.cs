namespace GTEK.FSM.WebPortal.Models;

/// <summary>
/// Generic result container for resilient data-fetch operations.
/// Carries state, data (if available), error details, and retry metadata.
/// </summary>
/// <typeparam name="T">The type of data being fetched.</typeparam>
public class FetchResult<T>
{
	public FetchState State { get; set; } = FetchState.Idle;

	/// <summary>
	/// The successfully fetched data (populated when State is Success or PartialSuccess or Stale).
	/// </summary>
	public T? Data { get; set; }

	/// <summary>
	/// Human-readable error message when State is Error or PartialSuccess.
	/// </summary>
	public string? ErrorMessage { get; set; }

	/// <summary>
	/// The exception that caused the failure, if any (for diagnostic purposes).
	/// </summary>
	public Exception? Exception { get; set; }

	/// <summary>
	/// Number of retry attempts already made.
	/// </summary>
	public int RetryAttempts { get; set; } = 0;

	/// <summary>
	/// Maximum retry attempts allowed before giving up.
	/// </summary>
	public int MaxRetries { get; set; } = 3;

	/// <summary>
	/// When the data was last successfully fetched (for staleness calculation).
	/// </summary>
	public DateTime? LastSuccessFetchedAt { get; set; }

	/// <summary>
	/// Time threshold (in seconds) after which data is considered stale.
	/// </summary>
	public int StaleThresholdSeconds { get; set; } = 300; // 5 minutes default

	/// <summary>
	/// Check if current data is stale based on LastSuccessFetchedAt and StaleThresholdSeconds.
	/// </summary>
	public bool IsStale =>
		LastSuccessFetchedAt.HasValue
		&& (DateTime.UtcNow - LastSuccessFetchedAt.Value).TotalSeconds > StaleThresholdSeconds;

	/// <summary>
	/// Check if more retries are available.
	/// </summary>
	public bool CanRetry => RetryAttempts < MaxRetries;

	/// <summary>
	/// Check if the result represents a successful state (fresh or stale data available).
	/// </summary>
	public bool HasData => Data != null && (State == FetchState.Success || State == FetchState.Stale || State == FetchState.PartialSuccess);
}
