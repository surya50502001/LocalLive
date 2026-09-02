using LocalLive.Domain.Common;
using LocalLive.Domain.Common.Services;

namespace LocalLive.Infrastructure.GeoSpatial;

public class HaversineDistanceCalculator : IDistanceCalculator
{
    private const double EarthRadiusMeters = 6371000.0;

    public double DistanceMeters(GeoPoint a, GeoPoint b)
    {
        var dLat = DegreesToRadians(b.Latitude - a.Latitude);
        var dLon = DegreesToRadians(b.Longitude - a.Longitude);

        var sLat = Math.Sin(dLat / 2);
        var sLon = Math.Sin(dLon / 2);
        var h = sLat * sLat + Math.Cos(DegreesToRadians(a.Latitude)) * Math.Cos(DegreesToRadians(b.Latitude)) * sLon * sLon;
        var c = 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));

        return EarthRadiusMeters * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}
