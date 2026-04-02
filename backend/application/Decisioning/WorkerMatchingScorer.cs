namespace GTEK.FSM.Backend.Application.Decisioning;

/// <summary>
/// Pure deterministic scorer for a single worker candidate.
/// </summary>
public static class WorkerMatchingScorer
{
    private const decimal MaxRating = 5.0m;

    /// <summary>
    /// Computes the weighted total score for a worker candidate.
    /// </summary>
    /// <param name="requiredSkills">Skills required by the service request (normalized, lower-case).</param>
    /// <param name="workerSkills">Skills the candidate worker possesses (normalized, lower-case).</param>
    /// <param name="activeJobCount">Number of currently active (PendingAcceptance or Accepted) jobs for the worker in the tenant.</param>
    /// <param name="internalRating">Worker's internal rating (expected range 0–5).</param>
    /// <param name="weights">Weighting configuration to apply.</param>
    /// <returns>Total score in [0, 1], higher is better.</returns>
    public static decimal ComputeScore(
        IReadOnlyList<string> requiredSkills,
        IReadOnlyList<string> workerSkills,
        int activeJobCount,
        decimal internalRating,
        WorkerMatchingWeights weights)
    {
        var skillScore = ComputeSkillScore(requiredSkills, workerSkills);
        var loadScore = ComputeLoadScore(activeJobCount);
        var ratingScore = ComputeRatingScore(internalRating);

        return (weights.SkillWeight * skillScore)
             + (weights.LoadWeight * loadScore)
             + (weights.RatingWeight * ratingScore);
    }

    /// <summary>
    /// Fraction of required skills matched by the worker. Returns 1.0 when none required.
    /// </summary>
    public static decimal ComputeSkillScore(
        IReadOnlyList<string> requiredSkills,
        IReadOnlyList<string> workerSkills)
    {
        if (requiredSkills.Count == 0)
        {
            return 1.0m;
        }

        var workerSet = new HashSet<string>(workerSkills, StringComparer.OrdinalIgnoreCase);
        var matched = requiredSkills.Count(s => workerSet.Contains(s));
        return (decimal)matched / requiredSkills.Count;
    }

    /// <summary>
    /// Load score is 1 / (1 + activeJobCount), so idle workers score 1.0 and busy workers approach 0.
    /// </summary>
    public static decimal ComputeLoadScore(int activeJobCount)
    {
        if (activeJobCount < 0)
        {
            activeJobCount = 0;
        }

        return 1.0m / (1 + activeJobCount);
    }

    /// <summary>
    /// Rating score = internalRating / MaxRating, clamped to [0, 1].
    /// </summary>
    public static decimal ComputeRatingScore(decimal internalRating)
    {
        var normalized = internalRating / MaxRating;
        return Math.Clamp(normalized, 0.0m, 1.0m);
    }
}
