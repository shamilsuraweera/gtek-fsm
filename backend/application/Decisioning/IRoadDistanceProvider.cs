namespace GTEK.FSM.Backend.Application.Decisioning;

public sealed record GeoCoordinate(decimal Latitude, decimal Longitude);

public sealed record RoadDistanceResult(decimal? DistanceKm, string Source)
{
    public static RoadDistanceResult Unavailable(string source = "Unavailable") => new(null, source);

    public bool IsAvailable => this.DistanceKm.HasValue;
}

public interface IRoadDistanceProvider
{
    Task<RoadDistanceResult> GetRoadDistanceAsync(
        GeoCoordinate origin,
        GeoCoordinate destination,
        CancellationToken cancellationToken = default);
}
