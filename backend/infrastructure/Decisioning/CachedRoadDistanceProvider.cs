using GTEK.FSM.Backend.Application.Decisioning;
using GTEK.FSM.Backend.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GTEK.FSM.Backend.Infrastructure.Decisioning;

public sealed class CachedRoadDistanceProvider : IRoadDistanceProvider
{
    private readonly IRoadDistanceProvider inner;
    private readonly IMemoryCache cache;
    private readonly IOptions<ExternalServicesOptions> options;

    public CachedRoadDistanceProvider(
        IRoadDistanceProvider inner,
        IMemoryCache cache,
        IOptions<ExternalServicesOptions> options)
    {
        this.inner = inner;
        this.cache = cache;
        this.options = options;
    }

    public async Task<RoadDistanceResult> GetRoadDistanceAsync(
        GeoCoordinate origin,
        GeoCoordinate destination,
        CancellationToken cancellationToken = default)
    {
        var key = BuildCacheKey(origin, destination);
        if (this.cache.TryGetValue<RoadDistanceResult>(key, out var cached)
            && cached is not null)
        {
            return cached;
        }

        var resolved = await this.inner.GetRoadDistanceAsync(origin, destination, cancellationToken);

        var ttlMinutes = Math.Max(1, this.options.Value.Maps.RouteCacheTtlMinutes);
        this.cache.Set(key, resolved, TimeSpan.FromMinutes(ttlMinutes));
        return resolved;
    }

    private static string BuildCacheKey(GeoCoordinate origin, GeoCoordinate destination)
    {
        return $"road-distance:{Math.Round(origin.Latitude, 4)}:{Math.Round(origin.Longitude, 4)}:{Math.Round(destination.Latitude, 4)}:{Math.Round(destination.Longitude, 4)}";
    }
}
