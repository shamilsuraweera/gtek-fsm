namespace GTEK.FSM.Backend.Application.Decisioning;

public interface IDecisioningMetricsCollector
{
    void RecordMatchEvaluation(
        Guid tenantId,
        DateTime observedAtUtc,
        long matchLatencyMs,
        int candidateCount,
        decimal? topCandidateScore);

    IReadOnlyList<DecisioningMatchMetricSample> GetMatchEvaluations(
        Guid tenantId,
        DateTime fromUtc,
        DateTime toUtc);
}
