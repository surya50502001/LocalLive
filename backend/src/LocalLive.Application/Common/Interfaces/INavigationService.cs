using LocalLive.Domain.Common;

namespace LocalLive.Application.Common.Interfaces;

public class NavigationRouteStep
{
    public string Instruction { get; set; } = string.Empty;
    public string Maneuver { get; set; } = "straight"; // straight, turn-left, turn-right, u-turn, arrive
    public double DistanceMeters { get; set; }
    public double DurationSeconds { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class NavigationRouteResult
{
    public double TotalDistanceMeters { get; set; }
    public double TotalDurationSeconds { get; set; }
    public string DistanceText { get; set; } = string.Empty;
    public string DurationText { get; set; } = string.Empty;
    public List<GeoPoint> PolylineCoordinates { get; set; } = new();
    public List<NavigationRouteStep> Steps { get; set; } = new();
    public string Mode { get; set; } = "walking"; // walking | driving
}

public interface INavigationService
{
    Task<NavigationRouteResult> CalculateRouteAsync(GeoPoint from, GeoPoint destination, string mode = "walking", CancellationToken ct = default);
}
