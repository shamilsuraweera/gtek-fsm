using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Backend.Domain.Enums;

namespace GTEK.FSM.Backend.Application.Decisioning;

internal sealed class WorkerMatchingService : IWorkerMatchingService
{
    private readonly IWorkerProfileRepository workerProfileRepository;
    private readonly IJobRepository jobRepository;

    public WorkerMatchingService(
        IWorkerProfileRepository workerProfileRepository,
        IJobRepository jobRepository)
    {
        this.workerProfileRepository = workerProfileRepository;
        this.jobRepository = jobRepository;
    }

    public async Task<IReadOnlyList<RankedWorkerCandidate>> GetRankedCandidatesAsync(
        WorkerMatchingQuery query,
        CancellationToken cancellationToken = default)
    {
        var topN = Math.Clamp(query.TopN, 1, WorkerMatchingQuery.MaxTopN);

        // Fetch all active workers for the tenant (no paging; matching scans the full set).
        var spec = new WorkerProfileQuerySpecification(
            TenantId: query.TenantId,
            IncludeInactive: false);

        var workers = await this.workerProfileRepository.QueryAsync(spec, cancellationToken);

        if (workers.Count == 0)
        {
            return Array.Empty<RankedWorkerCandidate>();
        }

        // Batch-load active job counts for all candidate workers.
        var workerIds = workers.Select(w => w.Id).ToList();
        var loadMap = await this.jobRepository.GetActiveJobCountsByWorkerAsync(
            query.TenantId, workerIds, cancellationToken);

        // Normalize required skills once.
        var requiredSkills = query.RequiredSkills
            .Select(s => s.Trim().ToLowerInvariant())
            .Where(s => s.Length > 0)
            .Distinct()
            .ToArray();

        var candidates = new List<RankedWorkerCandidate>(workers.Count);

        foreach (var worker in workers)
        {
            if (worker.AvailabilityStatus == WorkerAvailabilityStatus.Unavailable)
            {
                continue;
            }

            var workerSkills = worker.GetSkills()
                .Select(s => s.ToLowerInvariant())
                .ToArray();

            var activeCount = loadMap.TryGetValue(worker.Id, out var c) ? c : 0;

            var skillScore = WorkerMatchingScorer.ComputeSkillScore(requiredSkills, workerSkills);
            var loadScore = WorkerMatchingScorer.ComputeLoadScore(activeCount);
            var ratingScore = WorkerMatchingScorer.ComputeRatingScore(worker.InternalRating);
            var total = WorkerMatchingScorer.ComputeScore(
                requiredSkills, workerSkills, activeCount, worker.InternalRating, query.Weights);

            candidates.Add(new RankedWorkerCandidate(
                WorkerId: worker.Id,
                WorkerCode: worker.WorkerCode,
                DisplayName: worker.DisplayName,
                InternalRating: worker.InternalRating,
                Skills: worker.GetSkills(),
                ActiveJobCount: activeCount,
                TotalScore: total,
                SkillScore: skillScore,
                LoadScore: loadScore,
                RatingScore: ratingScore));
        }

        // Primary: total score descending. Tie-break: WorkerCode ascending (deterministic).
        return candidates
            .OrderByDescending(c => c.TotalScore)
            .ThenBy(c => c.WorkerCode, StringComparer.Ordinal)
            .Take(topN)
            .ToList();
    }
}
