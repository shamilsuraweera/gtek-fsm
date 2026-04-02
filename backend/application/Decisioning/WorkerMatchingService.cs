using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Backend.Domain.Enums;

namespace GTEK.FSM.Backend.Application.Decisioning;

internal sealed class WorkerMatchingService : IWorkerMatchingService
{
    private const decimal NeutralDistanceScore = 0.5m;
    private readonly IWorkerProfileRepository workerProfileRepository;
    private readonly IJobRepository jobRepository;
    private readonly IRoadDistanceProvider roadDistanceProvider;

    public WorkerMatchingService(
        IWorkerProfileRepository workerProfileRepository,
        IJobRepository jobRepository,
        IRoadDistanceProvider roadDistanceProvider)
    {
        this.workerProfileRepository = workerProfileRepository;
        this.jobRepository = jobRepository;
        this.roadDistanceProvider = roadDistanceProvider;
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

        var requestCoordinate = ResolveRequestCoordinate(query.RequestLatitude, query.RequestLongitude);

        var workingCandidates = new List<WorkingCandidate>(workers.Count);

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

            decimal? distanceKm = null;
            var distanceSource = "Unavailable";
            if (requestCoordinate is not null && worker.BaseLatitude.HasValue && worker.BaseLongitude.HasValue)
            {
                var workerCoordinate = new GeoCoordinate(worker.BaseLatitude.Value, worker.BaseLongitude.Value);
                var roadDistance = await this.roadDistanceProvider.GetRoadDistanceAsync(requestCoordinate, workerCoordinate, cancellationToken);

                if (roadDistance.IsAvailable)
                {
                    distanceKm = roadDistance.DistanceKm;
                    distanceSource = roadDistance.Source;
                }
                else
                {
                    distanceKm = ComputeStraightLineDistanceKm(requestCoordinate, workerCoordinate);
                    distanceSource = "FallbackStraightLine";
                }
            }

            workingCandidates.Add(new WorkingCandidate(
                WorkerId: worker.Id,
                WorkerCode: worker.WorkerCode,
                DisplayName: worker.DisplayName,
                InternalRating: worker.InternalRating,
                Skills: worker.GetSkills(),
                ActiveJobCount: activeCount,
                SkillScore: skillScore,
                LoadScore: loadScore,
                RatingScore: ratingScore,
                DistanceKm: distanceKm,
                DistanceSource: distanceSource));
        }

        var candidates = BuildRankedCandidates(workingCandidates, requiredSkills, query.Weights);

        // Primary: total score descending. Tie-break: WorkerCode ascending (deterministic).
        return candidates
            .OrderByDescending(c => c.TotalScore)
            .ThenBy(c => c.WorkerCode, StringComparer.Ordinal)
            .Take(topN)
            .ToList();
    }

    private static IReadOnlyList<RankedWorkerCandidate> BuildRankedCandidates(
        IReadOnlyList<WorkingCandidate> candidates,
        IReadOnlyList<string> requiredSkills,
        WorkerMatchingWeights weights)
    {
        if (candidates.Count == 0)
        {
            return Array.Empty<RankedWorkerCandidate>();
        }

        var availableDistances = candidates
            .Where(x => x.DistanceKm.HasValue)
            .Select(x => x.DistanceKm!.Value)
            .ToArray();

        decimal minDistance = 0m;
        decimal maxDistance = 0m;
        if (availableDistances.Length > 0)
        {
            minDistance = availableDistances.Min();
            maxDistance = availableDistances.Max();
        }

        var ranked = new List<RankedWorkerCandidate>(candidates.Count);
        foreach (var candidate in candidates)
        {
            var distanceScore = ComputeDistanceScore(candidate.DistanceKm, minDistance, maxDistance);
            var total = WorkerMatchingScorer.ComputeScore(
                requiredSkills,
                candidate.Skills.Select(x => x.ToLowerInvariant()).ToArray(),
                candidate.ActiveJobCount,
                candidate.InternalRating,
                distanceScore,
                weights);

            ranked.Add(new RankedWorkerCandidate(
                WorkerId: candidate.WorkerId,
                WorkerCode: candidate.WorkerCode,
                DisplayName: candidate.DisplayName,
                InternalRating: candidate.InternalRating,
                Skills: candidate.Skills,
                ActiveJobCount: candidate.ActiveJobCount,
                TotalScore: total,
                SkillScore: candidate.SkillScore,
                LoadScore: candidate.LoadScore,
                RatingScore: candidate.RatingScore,
                DistanceScore: distanceScore,
                DistanceKm: candidate.DistanceKm,
                DistanceSource: candidate.DistanceSource));
        }

        return ranked;
    }

    private static GeoCoordinate? ResolveRequestCoordinate(decimal? latitude, decimal? longitude)
    {
        if (latitude.HasValue != longitude.HasValue)
        {
            return null;
        }

        if (!latitude.HasValue)
        {
            return null;
        }

        return new GeoCoordinate(latitude.Value, longitude!.Value);
    }

    private static decimal ComputeDistanceScore(decimal? distanceKm, decimal minDistance, decimal maxDistance)
    {
        if (!distanceKm.HasValue)
        {
            return NeutralDistanceScore;
        }

        if (maxDistance <= minDistance)
        {
            return 1.0m;
        }

        var normalized = (distanceKm.Value - minDistance) / (maxDistance - minDistance);
        return Math.Clamp(1.0m - normalized, 0.0m, 1.0m);
    }

    private static decimal ComputeStraightLineDistanceKm(GeoCoordinate origin, GeoCoordinate destination)
    {
        const double earthRadiusKm = 6371.0;

        double dLat = ToRadians((double)(destination.Latitude - origin.Latitude));
        double dLon = ToRadians((double)(destination.Longitude - origin.Longitude));
        double lat1 = ToRadians((double)origin.Latitude);
        double lat2 = ToRadians((double)destination.Latitude);

        double a = Math.Pow(Math.Sin(dLat / 2.0), 2.0)
            + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLon / 2.0), 2.0);

        double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
        var distance = earthRadiusKm * c;
        return Math.Round((decimal)distance, 3, MidpointRounding.AwayFromZero);
    }

    private static double ToRadians(double degree)
    {
        return degree * Math.PI / 180.0;
    }

    private sealed record WorkingCandidate(
        Guid WorkerId,
        string WorkerCode,
        string DisplayName,
        decimal InternalRating,
        IReadOnlyList<string> Skills,
        int ActiveJobCount,
        decimal SkillScore,
        decimal LoadScore,
        decimal RatingScore,
        decimal? DistanceKm,
        string DistanceSource);
}
