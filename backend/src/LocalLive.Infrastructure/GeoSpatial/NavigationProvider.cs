using LocalLive.Domain.Common;
using LocalLive.Domain.Common.Services;
using Microsoft.Extensions.Options;

namespace LocalLive.Infrastructure.GeoSpatial;

public class NavigationOptions
{
    /// <summary>google_maps | apple_maps | waze | osm | none</summary>
    public string Provider { get; set; } = "google_maps";
}

/// <summary>
/// Builds deep-link navigation URLs. The provider is configurable via
/// environment so business logic is not tied to a single map provider.
/// </summary>
public class NavigationProvider : INavigationProvider
{
    private readonly NavigationOptions _options;

    public NavigationProvider(IOptions<NavigationOptions> options)
    {
        _options = options.Value;
    }

    public string BuildNavigationUrl(GeoPoint from, GeoPoint destination, string placeLabel)
    {
        var label = Uri.EscapeDataString(placeLabel);
        var origin = $"{from.Latitude:F6},{from.Longitude:F6}";
        var dest = $"{destination.Latitude:F6},{destination.Longitude:F6}";

        return _options.Provider.ToLowerInvariant() switch
        {
            "apple_maps" => $"https://maps.apple.com/?daddr={dest}&dirflg=d",
            "waze" => $"https://waze.com/ul?ll={dest}&navigate=yes",
            "osm" => $"https://www.openstreetmap.org/directions?from={origin}&to={dest}",
            "none" => $"geo:{destination.Latitude:F6},{destination.Longitude:F6}?q={dest}({label})",
            _ => $"https://www.google.com/maps/dir/?api=1&origin={origin}&destination={dest}&travelmode=driving"
        };
    }
}
