using LocalLive.Domain.Common;

namespace LocalLive.Domain.Common.Services;

public interface IDistanceCalculator
{
    /// <summary>
    /// Distance between two points using the haversine formula (great-circle distance).
    /// Returns distance in metres.
    /// </summary>
    double DistanceMeters(GeoPoint a, GeoPoint b);
}

public interface INavigationProvider
{
    /// <summary>
    /// Build a deep-link URL to navigate from the customer's location to the shop.
    /// Implementations are configured via environment/options so the map provider
    /// is not hardcoded in business logic.
    /// </summary>
    string BuildNavigationUrl(GeoPoint from, GeoPoint destination, string placeLabel);
}
