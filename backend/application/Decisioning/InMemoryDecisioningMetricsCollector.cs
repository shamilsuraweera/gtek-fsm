namespace GTEK.FSM.Backend.Application.Decisioning;

/// <summary>
/// In-memory collector for decisioning metrics used by management reporting.
/// </summary>
internal sealed class InMemoryDecisioningMetricsCollector : IDecisioningMetricsCollector
{
    private const int MaxRetainedSamples = 10000;
    private readonly object gate = new();
    private readonly Queue<DecisioningMatchMetricSample> samples = new();

    /// <summary>
    /// Records a single worker-matching evaluation sample.
    /// </summary>
    public void RecordMatchEvaluation(
        Guid tenantId,
        DateTime observedAtUtc,
        long matchLatencyMs,
        int candidateCount,
        decimal? topCandidateScore)
    {
        var normalizedObservedAtUtc = observedAtUtc.Kind == DateTimeKind.Utc
            ? observedAtUtc
            : DateTime.SpecifyKind(observedAtUtc, DateTimeKind.Utc);

        var sample = new DecisioningMatchMetricSample(
            TenantId: tenantId,
            ObservedAtUtc: normalizedObservedAtUtc,
            MatchLatencyMs: Math.Max(0L, matchLatencyMs),
            CandidateCount: Math.Max(0, candidateCount),
            TopCandidateScore: topCandidateScore);

        lock (this.gate)
        {
            this.samples.Enqueue(sample);
            while (this.samples.Count > MaxRetainedSamples)
            {
                this.samples.Dequeue();
            }
        }
    }

    /// <summary>
    /// Returns matching evaluation samples in the requested tenant and time window.
    /// </summary>
    public IReadOnlyList<DecisioningMatchMetricSample> GetMatchEvaluations(Guid tenantId, DateTime fromUtc, DateTime toUtc)
    {
        lock (this.gate)
        {
            return this.samples
                .Where(x => x.TenantId == tenantId && x.ObservedAtUtc >= fromUtc && x.ObservedAtUtc <= toUtc)
                .OrderBy(x => x.ObservedAtUtc)
                .ToArray();
        }
    }
}
