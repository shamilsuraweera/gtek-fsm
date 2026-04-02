using GTEK.FSM.Backend.Application.Decisioning;
using GTEK.FSM.Backend.Infrastructure.Configuration;
using GTEK.FSM.Backend.Infrastructure.Decisioning;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Decisioning;

public sealed class CachedRoadDistanceProviderTests
{
    [Fact]
    public async Task GetRoadDistanceAsync_ReusesCachedValue_OnSecondCall()
    {
        var inner = new CountingDistanceProvider(new RoadDistanceResult(12.345m, "Road"));
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new ExternalServicesOptions
        {
            Maps = new MapsServiceOptions
            {
                RouteCacheTtlMinutes = 10,
            },
        });

        var sut = new CachedRoadDistanceProvider(inner, memoryCache, options);
        var origin = new GeoCoordinate(6.9271m, 79.8612m);
        var destination = new GeoCoordinate(7.2906m, 80.6337m);

        var first = await sut.GetRoadDistanceAsync(origin, destination);
        var second = await sut.GetRoadDistanceAsync(origin, destination);

        Assert.Equal(1, inner.CallCount);
        Assert.Equal(first.DistanceKm, second.DistanceKm);
        Assert.Equal("Road", second.Source);
    }

    private sealed class CountingDistanceProvider : IRoadDistanceProvider
    {
        private readonly RoadDistanceResult result;

        public CountingDistanceProvider(RoadDistanceResult result)
        {
            this.result = result;
        }

        public int CallCount { get; private set; }

        public Task<RoadDistanceResult> GetRoadDistanceAsync(
            GeoCoordinate origin,
            GeoCoordinate destination,
            CancellationToken cancellationToken = default)
        {
            this.CallCount++;
            return Task.FromResult(this.result);
        }
    }
}
