using GTEK.FSM.Backend.Application.Decisioning;
using GTEK.FSM.Backend.Infrastructure.Configuration;
using GTEK.FSM.Backend.Infrastructure.Decisioning;
using Microsoft.Extensions.Options;
using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Decisioning;

public sealed class OsrmRoadDistanceProviderTests
{
    [Fact]
    public async Task GetRoadDistanceAsync_WhenMapsDisabled_ReturnsUnavailable()
    {
        var options = Options.Create(new ExternalServicesOptions
        {
            Maps = new MapsServiceOptions
            {
                Enabled = false,
                Provider = "OSRM",
                BaseUrl = "http://localhost",
            },
        });

        var sut = new OsrmRoadDistanceProvider(new StubHttpClientFactory(), options);
        var result = await sut.GetRoadDistanceAsync(new GeoCoordinate(6.9m, 79.8m), new GeoCoordinate(7.2m, 80.7m));

        Assert.False(result.IsAvailable);
        Assert.Equal("Unavailable", result.Source);
    }

    [Fact]
    public async Task GetRoadDistanceAsync_WhenProviderNotOsrm_ReturnsUnavailable()
    {
        var options = Options.Create(new ExternalServicesOptions
        {
            Maps = new MapsServiceOptions
            {
                Enabled = true,
                Provider = "None",
                BaseUrl = "http://localhost",
            },
        });

        var sut = new OsrmRoadDistanceProvider(new StubHttpClientFactory(), options);
        var result = await sut.GetRoadDistanceAsync(new GeoCoordinate(6.9m, 79.8m), new GeoCoordinate(7.2m, 80.7m));

        Assert.False(result.IsAvailable);
        Assert.Equal("Unavailable", result.Source);
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient();
        }
    }
}
