namespace GTEK.FSM.Backend.Application.Decisioning;

public sealed record DecisioningMatchMetricSample(
    Guid TenantId,
    DateTime ObservedAtUtc,
    long MatchLatencyMs,
    int CandidateCount,
    decimal? TopCandidateScore);
