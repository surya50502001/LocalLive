using FluentAssertions;
using LocalLive.Domain.Common;
using LocalLive.Infrastructure.GeoSpatial;
using Xunit;

namespace LocalLive.Tests;

public class HaversineDistanceCalculatorTests
{
    private readonly HaversineDistanceCalculator _calculator = new();

    [Fact]
    public void DistanceMeters_SamePoint_ShouldReturnZero()
    {
        var point = new GeoPoint(12.9716, 77.5946);
        var distance = _calculator.DistanceMeters(point, point);

        distance.Should().BeApproximately(0, 0.01);
    }

    [Fact]
    public void DistanceMeters_KnownCoordinates_ShouldCalculateAccurateDistance()
    {
        // Bangalore (12.9716, 77.5946) to Mysore (12.2958, 76.6394) is ~128 km
        var p1 = new GeoPoint(12.9716, 77.5946);
        var p2 = new GeoPoint(12.2958, 76.6394);

        var distanceM = _calculator.DistanceMeters(p1, p2);
        var distanceKm = distanceM / 1000.0;

        distanceKm.Should().BeInRange(125, 135);
    }

    [Fact]
    public void DistanceMeters_ShortHyperlocalDistance_ShouldBeAccurateWithin5Meters()
    {
        // Two points ~500 meters apart
        var p1 = new GeoPoint(12.9716, 77.5946);
        var p2 = new GeoPoint(12.9760, 77.5946); // ~489 meters north

        var distanceM = _calculator.DistanceMeters(p1, p2);
        distanceM.Should().BeInRange(480, 500);
    }
}
