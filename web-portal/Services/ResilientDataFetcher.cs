using GTEK.FSM.WebPortal.Models;

namespace GTEK.FSM.WebPortal.Services;

/// <summary>
/// Service for resilient data fetching with built-in retry logic, state tracking, and partial-failure recovery.
/// </summary>
public class ResilientDataFetcher
{
    /// <summary>
    /// Execute a fetch operation with automatic retry and staleness tracking.
    /// </summary>
    /// <typeparam name="T">The type of data to fetch.</typeparam>
    /// <param name="fetchFunc">Async function that performs the fetch. Throws on failure.</param>
    /// <param name="currentResult">The current FetchResult to update (enables stale data reuse).</param>
    /// <param name="maxRetries">Maximum retry attempts. Default: 3.</param>
    /// <param name="staleThresholdSeconds">Seconds after which data is considered stale. Default: 300 (5 min).</param>
    /// <param name="retryDelayMs">Delay between retry attempts in milliseconds.</param>
    /// <returns>Updated FetchResult with final state, data (if any), and error details.</returns>
    public async Task<FetchResult<T>> FetchAsync<T>(
        Func<Task<T>> fetchFunc,
        FetchResult<T>? currentResult = null,
        int maxRetries = 3,
        int staleThresholdSeconds = 300,
        int retryDelayMs = 500)
    {
        var result = currentResult ?? new FetchResult<T> { StaleThresholdSeconds = staleThresholdSeconds };
        result.MaxRetries = maxRetries;
        result.State = FetchState.Loading;

        try
        {
            var data = await fetchFunc();
            result.Data = data;
            result.State = FetchState.Success;
            result.LastSuccessFetchedAt = DateTime.UtcNow;
            result.RetryAttempts = 0;
            result.ErrorMessage = null;
            result.Exception = null;
            return result;
        }
        catch (Exception ex)
        {
            result.Exception = ex;

            // If this is a retry and we have previous data, mark as stale instead of hard error.
            if (result.RetryAttempts > 0 && result.Data != null)
            {
                result.State = FetchState.Stale;
                result.ErrorMessage = $"Fetch failed after retry attempt {result.RetryAttempts}; showing cached data.";
                return result;
            }

            // If we haven't exhausted retries, try again after a delay.
            if (result.RetryAttempts < maxRetries)
            {
                result.State = FetchState.Retrying;
                result.RetryAttempts++;
                result.ErrorMessage = $"Fetch failed; automatic retry {result.RetryAttempts} of {maxRetries} in progress...";

                await Task.Delay(retryDelayMs);
                return await this.FetchAsync(fetchFunc, result, maxRetries, staleThresholdSeconds, retryDelayMs);
            }

            // All retries exhausted.
            result.State = FetchState.Error;
            result.ErrorMessage = $"Fetch failed after {maxRetries} retries: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Execute a fetch operation with partial-failure handling (e.g., some items succeed, others fail).
    /// </summary>
    /// <typeparam name="T">The collection item type.</typeparam>
    /// <param name="fetchFunc">Async function returning a list. Can throw or return partial results.</param>
    /// <param name="currentResult">The current FetchResult to update.</param>
    /// <param name="successCountThreshold">Minimum successful items before returning PartialSuccess instead of Error.</param>
    /// <returns>Updated FetchResult with Success, PartialSuccess, Stale, or Error state.</returns>
    public async Task<FetchResult<IReadOnlyList<T>>> FetchCollectionWithPartialFailureAsync<T>(
        Func<Task<(IReadOnlyList<T> Successes, int TotalAttempted, string? ErrorDetails)>> fetchFunc,
        FetchResult<IReadOnlyList<T>>? currentResult = null,
        int successCountThreshold = 1)
    {
        var result = currentResult ?? new FetchResult<IReadOnlyList<T>>();
        result.State = FetchState.Loading;

        try
        {
            var (successes, totalAttempted, errorDetails) = await fetchFunc();

            if (successes.Count == 0)
            {
                result.State = FetchState.Error;
                result.ErrorMessage = errorDetails ?? "No data retrieved.";
                return result;
            }

            if (successes.Count < totalAttempted && successes.Count >= successCountThreshold)
            {
                result.State = FetchState.PartialSuccess;
                result.Data = successes;
                result.LastSuccessFetchedAt = DateTime.UtcNow;
                result.ErrorMessage = errorDetails ?? $"Retrieved {successes.Count} of {totalAttempted} items.";
                return result;
            }

            result.State = FetchState.Success;
            result.Data = successes;
            result.LastSuccessFetchedAt = DateTime.UtcNow;
            result.ErrorMessage = null;
            return result;
        }
        catch (Exception ex)
        {
            result.State = FetchState.Error;
            result.ErrorMessage = $"Collection fetch failed: {ex.Message}";
            result.Exception = ex;
            return result;
        }
    }

    /// <summary>
    /// Manually trigger a retry on a stale or failed FetchResult.
    /// </summary>
    /// <typeparam name="T">The type of data to fetch.</typeparam>
    /// <param name="fetchFunc">Async function that performs the fetch.</param>
    /// <param name="currentResult">The current fetch result used as the retry baseline.</param>
    /// <param name="retryDelayMs">Delay between retry attempts in milliseconds.</param>
    /// <returns>Updated FetchResult after the retry attempt completes.</returns>
    public async Task<FetchResult<T>> RetryAsync<T>(
        Func<Task<T>> fetchFunc,
        FetchResult<T> currentResult,
        int retryDelayMs = 500)
    {
        if (!currentResult.CanRetry && currentResult.State != FetchState.Stale)
        {
            currentResult.ErrorMessage = "Maximum retries exceeded; cannot retry further.";
            return currentResult;
        }

        // Reset retry counter for manual retry.
        currentResult.RetryAttempts = 0;
        return await this.FetchAsync(fetchFunc, currentResult, currentResult.MaxRetries, currentResult.StaleThresholdSeconds, retryDelayMs);
    }
}
