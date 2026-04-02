namespace GTEK.FSM.Backend.Application.Decisioning;

/// <summary>
/// A single ranked worker candidate returned by the matching engine.
/// </summary>
public sealed record RankedWorkerCandidate(
    Guid WorkerId,
    string WorkerCode,
    string DisplayName,
    decimal InternalRating,
    IReadOnlyList<string> Skills,
    int ActiveJobCount,
    decimal TotalScore,
    decimal SkillScore,
    decimal LoadScore,
    decimal RatingScore);

/// <summary>
/// Query parameters for requesting ranked worker candidates.
/// </summary>
public sealed record WorkerMatchingQuery(
    Guid TenantId,
    IReadOnlyList<string> RequiredSkills,
    int TopN,
    WorkerMatchingWeights Weights)
{
    public const int DefaultTopN = 10;
    public const int MaxTopN = 50;
}
