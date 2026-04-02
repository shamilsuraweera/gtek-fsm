namespace GTEK.FSM.Backend.Application.Decisioning;

public interface IWorkerMatchingService
{
    Task<IReadOnlyList<RankedWorkerCandidate>> GetRankedCandidatesAsync(
        WorkerMatchingQuery query,
        CancellationToken cancellationToken = default);
}
