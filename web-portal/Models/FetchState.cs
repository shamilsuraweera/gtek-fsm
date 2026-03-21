namespace GTEK.FSM.WebPortal.Models;

/// <summary>
/// Represents the lifecycle state of a data-fetch operation.
/// </summary>
public enum FetchState
{
	/// <summary>Initial state before any fetch attempt.</summary>
	Idle = 0,

	/// <summary>Actively fetching from the source.</summary>
	Loading = 1,

	/// <summary>Fetch succeeded; current data is fresh.</summary>
	Success = 2,

	/// <summary>Fetch succeeded earlier; data exists but may be stale (retry offered).</summary>
	Stale = 3,

	/// <summary>Fetch failed; no recovery attempt in progress.</summary>
	Error = 4,

	/// <summary>Automatic or manual retry is in progress.</summary>
	Retrying = 5,

	/// <summary>Partial data available; some items succeeded, some failed.</summary>
	PartialSuccess = 6,
}
