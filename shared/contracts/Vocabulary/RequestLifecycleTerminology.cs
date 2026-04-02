namespace GTEK.FSM.Shared.Contracts.Vocabulary;

/// <summary>
/// Provides shared lifecycle terminology normalization and display mapping.
/// </summary>
public static class RequestLifecycleTerminology
{
    /// <summary>
    /// Normalizes a lifecycle status string into canonical request lifecycle values.
    /// </summary>
    /// <param name="rawStatus">Raw lifecycle status from API, realtime, or UI state.</param>
    /// <returns>Canonical lifecycle status value.</returns>
    public static string NormalizeStatus(string? rawStatus)
    {
        if (string.IsNullOrWhiteSpace(rawStatus))
        {
            return RequestStage.New.ToString();
        }

        var normalizedInput = rawStatus.Trim();

        if (Enum.TryParse<RequestStage>(normalizedInput, ignoreCase: true, out var parsedStage))
        {
            return parsedStage.ToString();
        }

        var token = NormalizeToken(normalizedInput);
        return token switch
        {
            "new" or "pending" or "submitted" or "intake" or "available" => RequestStage.New.ToString(),
            "assigned" or "dispatch" or "scheduled" => RequestStage.Assigned.ToString(),
            "inprogress" or "active" => RequestStage.InProgress.ToString(),
            "onhold" or "hold" or "waiting" or "assessment" => RequestStage.OnHold.ToString(),
            "completed" or "resolved" or "done" or "closed" => RequestStage.Completed.ToString(),
            "cancelled" or "canceled" => RequestStage.Cancelled.ToString(),
            _ => normalizedInput,
        };
    }

    /// <summary>
    /// Maps a lifecycle status value to a consistent user-facing label.
    /// </summary>
    /// <param name="rawStatus">Raw lifecycle status from API, realtime, or UI state.</param>
    /// <returns>Display label for lifecycle status.</returns>
    public static string GetDisplayLabel(string? rawStatus)
    {
        var normalized = NormalizeStatus(rawStatus);
        return normalized switch
        {
            nameof(RequestStage.InProgress) => "In Progress",
            nameof(RequestStage.OnHold) => "On Hold",
            _ => normalized,
        };
    }

    /// <summary>
    /// Resolves a timeline stage index from a lifecycle status.
    /// </summary>
    /// <param name="rawStatus">Raw lifecycle status from API, realtime, or UI state.</param>
    /// <returns>Timeline stage index.</returns>
    public static int ResolveStageIndex(string? rawStatus)
    {
        var normalized = NormalizeStatus(rawStatus);
        return normalized switch
        {
            nameof(RequestStage.New) => 0,
            nameof(RequestStage.Assigned) => 1,
            nameof(RequestStage.InProgress) => 2,
            nameof(RequestStage.OnHold) => 2,
            nameof(RequestStage.Completed) => 3,
            nameof(RequestStage.Cancelled) => 3,
            _ => 0,
        };
    }

    private static string NormalizeToken(string value)
    {
        return value
            .Trim()
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty)
            .ToLowerInvariant();
    }
}
