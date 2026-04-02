using System.Text.Json;
using GTEK.FSM.Backend.Application.Decisioning;
using GTEK.FSM.Backend.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace GTEK.FSM.Backend.Infrastructure.Decisioning;

public sealed class OsrmRoadDistanceProvider : IRoadDistanceProvider
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IOptions<ExternalServicesOptions> options;

    public OsrmRoadDistanceProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<ExternalServicesOptions> options)
    {
        this.httpClientFactory = httpClientFactory;
        this.options = options;
    }

    public async Task<RoadDistanceResult> GetRoadDistanceAsync(
        GeoCoordinate origin,
        GeoCoordinate destination,
        CancellationToken cancellationToken = default)
    {
        var maps = this.options.Value.Maps;
        if (!maps.Enabled)
        {
            return RoadDistanceResult.Unavailable("Unavailable");
        }

        if (!maps.Provider.Equals("OSRM", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(maps.BaseUrl))
        {
            return RoadDistanceResult.Unavailable("Unavailable");
        }

        try
        {
            var client = this.httpClientFactory.CreateClient(nameof(OsrmRoadDistanceProvider));
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, maps.RequestTimeoutSeconds));

            var requestUri = BuildRouteUri(maps.BaseUrl, origin, destination);
            using var response = await client.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return RoadDistanceResult.Unavailable("Unavailable");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!doc.RootElement.TryGetProperty("routes", out var routes)
                || routes.ValueKind != JsonValueKind.Array
                || routes.GetArrayLength() == 0)
            {
                return RoadDistanceResult.Unavailable("Unavailable");
            }

            var firstRoute = routes[0];
            if (!firstRoute.TryGetProperty("distance", out var distanceMetersElement)
                || distanceMetersElement.ValueKind != JsonValueKind.Number)
            {
                return RoadDistanceResult.Unavailable("Unavailable");
            }

            var distanceMeters = distanceMetersElement.GetDecimal();
            var distanceKm = Math.Round(distanceMeters / 1000m, 3, MidpointRounding.AwayFromZero);
            return new RoadDistanceResult(distanceKm, "Road");
        }
        catch
        {
            return RoadDistanceResult.Unavailable("Unavailable");
        }
    }

    private static string BuildRouteUri(string baseUrl, GeoCoordinate origin, GeoCoordinate destination)
    {
        var trimmed = baseUrl.TrimEnd('/');
        return $"{trimmed}/route/v1/driving/{origin.Longitude},{origin.Latitude};{destination.Longitude},{destination.Latitude}?overview=false";
    }
}
