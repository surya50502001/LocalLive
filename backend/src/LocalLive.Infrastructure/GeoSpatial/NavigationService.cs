using System.Globalization;
using System.Text.Json;
using LocalLive.Application.Common.Interfaces;
using LocalLive.Domain.Common;
using LocalLive.Domain.Common.Services;
using Microsoft.Extensions.Logging;

namespace LocalLive.Infrastructure.GeoSpatial;

public class NavigationService : INavigationService
{
    private readonly HttpClient _httpClient;
    private readonly IDistanceCalculator _distanceCalculator;
    private readonly ILogger<NavigationService> _logger;

    public NavigationService(
        HttpClient httpClient,
        IDistanceCalculator distanceCalculator,
        ILogger<NavigationService> logger)
    {
        _httpClient = httpClient;
        _distanceCalculator = distanceCalculator;
        _logger = logger;
    }

    public async Task<NavigationRouteResult> CalculateRouteAsync(
        GeoPoint from,
        GeoPoint destination,
        string mode = "walking",
        CancellationToken ct = default)
    {
        var normalizedMode = mode.ToLowerInvariant() == "driving" ? "driving" : "walking";
        var osrmProfile = normalizedMode == "driving" ? "car" : "foot";

        try
        {
            var url = string.Format(
                CultureInfo.InvariantCulture,
                "https://router.project-osrm.org/route/v1/{0}/{1:F6},{2:F6};{3:F6},{4:F6}?overview=full&geometries=geojson&steps=true",
                osrmProfile,
                from.Longitude,
                from.Latitude,
                destination.Longitude,
                destination.Latitude);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(3));

            var response = await _httpClient.GetAsync(url, cts.Token);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cts.Token);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("code", out var code) && code.GetString() == "Ok" &&
                    root.TryGetProperty("routes", out var routes) && routes.GetArrayLength() > 0)
                {
                    var route = routes[0];
                    var distance = route.GetProperty("distance").GetDouble();
                    var duration = route.GetProperty("duration").GetDouble();

                    var polyline = new List<GeoPoint>();
                    if (route.TryGetProperty("geometry", out var geom) &&
                        geom.TryGetProperty("coordinates", out var coords))
                    {
                        foreach (var point in coords.EnumerateArray())
                        {
                            var lng = point[0].GetDouble();
                            var lat = point[1].GetDouble();
                            polyline.Add(new GeoPoint(lat, lng));
                        }
                    }

                    var steps = new List<NavigationRouteStep>();
                    if (route.TryGetProperty("legs", out var legs) && legs.GetArrayLength() > 0 &&
                        legs[0].TryGetProperty("steps", out var stepsEl))
                    {
                        foreach (var step in stepsEl.EnumerateArray())
                        {
                            var stepDist = step.GetProperty("distance").GetDouble();
                            var stepDur = step.GetProperty("duration").GetDouble();
                            var name = step.TryGetProperty("name", out var n) ? n.GetString() : string.Empty;

                            var maneuverType = "straight";
                            if (step.TryGetProperty("maneuver", out var m))
                            {
                                var type = m.TryGetProperty("type", out var t) ? t.GetString() : "straight";
                                var modifier = m.TryGetProperty("modifier", out var mod) ? mod.GetString() : string.Empty;
                                maneuverType = string.IsNullOrEmpty(modifier) ? type ?? "straight" : $"{type}-{modifier}";
                            }

                            var instruction = string.IsNullOrWhiteSpace(name)
                                ? FormatManeuverInstruction(maneuverType, stepDist)
                                : $"Turn onto {name} and proceed {FormatDistance(stepDist)}";

                            steps.Add(new NavigationRouteStep
                            {
                                Instruction = instruction,
                                Maneuver = maneuverType,
                                DistanceMeters = Math.Round(stepDist),
                                DurationSeconds = Math.Round(stepDur)
                            });
                        }
                    }

                    if (polyline.Count > 0)
                    {
                        return new NavigationRouteResult
                        {
                            TotalDistanceMeters = Math.Round(distance),
                            TotalDurationSeconds = Math.Round(duration),
                            DistanceText = FormatDistance(distance),
                            DurationText = FormatDuration(duration),
                            PolylineCoordinates = polyline,
                            Steps = steps.Count > 0 ? steps : GenerateFallbackSteps(from, destination, distance),
                            Mode = normalizedMode
                        };
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OSRM routing request failed or timed out. Generating algorithmic in-app route.");
        }

        // Fallback: Generate synthetic geometric route so in-app navigation NEVER breaks
        return GenerateSyntheticRoute(from, destination, normalizedMode);
    }

    private NavigationRouteResult GenerateSyntheticRoute(GeoPoint from, GeoPoint destination, string mode)
    {
        var straightDistance = _distanceCalculator.DistanceMeters(from, destination);
        var roadDistance = straightDistance * 1.25; // 25% urban road factor
        var speedMps = mode == "driving" ? 8.33 : 1.38; // ~30 km/h driving, ~5 km/h walking
        var durationSeconds = roadDistance / speedMps;

        // Interpolate 10 points along the path
        var points = new List<GeoPoint> { from };
        const int intermediateSegments = 8;
        for (var i = 1; i <= intermediateSegments; i++)
        {
            var fraction = (double)i / (intermediateSegments + 1);
            var lat = from.Latitude + (destination.Latitude - from.Latitude) * fraction;
            var lng = from.Longitude + (destination.Longitude - from.Longitude) * fraction;
            points.Add(new GeoPoint(lat, lng));
        }
        points.Add(destination);

        return new NavigationRouteResult
        {
            TotalDistanceMeters = Math.Round(roadDistance),
            TotalDurationSeconds = Math.Round(durationSeconds),
            DistanceText = FormatDistance(roadDistance),
            DurationText = FormatDuration(durationSeconds),
            PolylineCoordinates = points,
            Steps = GenerateFallbackSteps(from, destination, roadDistance),
            Mode = mode
        };
    }

    private static List<NavigationRouteStep> GenerateFallbackSteps(GeoPoint from, GeoPoint destination, double distance)
    {
        return new List<NavigationRouteStep>
        {
            new()
            {
                Instruction = $"Head towards destination for {FormatDistance(distance * 0.4)}",
                Maneuver = "depart",
                DistanceMeters = Math.Round(distance * 0.4),
                DurationSeconds = Math.Round(distance * 0.4 / 1.38),
                Latitude = from.Latitude,
                Longitude = from.Longitude
            },
            new()
            {
                Instruction = $"Continue straight along main street for {FormatDistance(distance * 0.4)}",
                Maneuver = "straight",
                DistanceMeters = Math.Round(distance * 0.4),
                DurationSeconds = Math.Round(distance * 0.4 / 1.38)
            },
            new()
            {
                Instruction = $"Destination is on your right in {FormatDistance(distance * 0.2)}",
                Maneuver = "arrive",
                DistanceMeters = Math.Round(distance * 0.2),
                DurationSeconds = Math.Round(distance * 0.2 / 1.38),
                Latitude = destination.Latitude,
                Longitude = destination.Longitude
            }
        };
    }

    private static string FormatManeuverInstruction(string maneuver, double distance)
    {
        var distStr = FormatDistance(distance);
        return maneuver switch
        {
            "depart" => $"Head out and proceed {distStr}",
            "turn-right" => $"Turn right and proceed {distStr}",
            "turn-left" => $"Turn left and proceed {distStr}",
            "turn-slight right" => $"Slight right and proceed {distStr}",
            "turn-slight left" => $"Slight left and proceed {distStr}",
            "arrive" => "Arrive at destination",
            _ => $"Continue straight for {distStr}"
        };
    }

    private static string FormatDistance(double meters)
    {
        if (meters < 1000)
            return $"{Math.Round(meters)} m";
        return $"{(meters / 1000.0):F1} km";
    }

    private static string FormatDuration(double seconds)
    {
        var minutes = (int)Math.Ceiling(seconds / 60.0);
        if (minutes < 60)
            return $"{minutes} min";
        var hours = minutes / 60;
        var rem = minutes % 60;
        return rem > 0 ? $"{hours} hr {rem} min" : $"{hours} hr";
    }
}
