using FluentAssertions;
using LocalLive.Domain.Common;
using LocalLive.Infrastructure.GeoSpatial;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LocalLive.Tests;

public class NavigationServiceTests
{
    private readonly NavigationService _navigationService;

    public NavigationServiceTests()
    {
        var httpClient = new HttpClient();
        var distanceCalculator = new HaversineDistanceCalculator();
        var logger = NullLogger<NavigationService>.Instance;

        _navigationService = new NavigationService(httpClient, distanceCalculator, logger);
    }

    [Fact]
    public async Task CalculateRouteAsync_ShouldReturnPolylineAndSteps()
    {
        var origin = new GeoPoint(12.9716, 77.5946);
        var dest = new GeoPoint(12.9750, 77.5990);

        var result = await _navigationService.CalculateRouteAsync(origin, dest, "walking");

        result.Should().NotBeNull();
        result.TotalDistanceMeters.Should().BeGreaterThan(0);
        result.TotalDurationSeconds.Should().BeGreaterThan(0);
        result.PolylineCoordinates.Should().NotBeEmpty();
        result.Steps.Should().NotBeEmpty();
        result.DistanceText.Should().NotBeNullOrWhiteSpace();
        result.DurationText.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CalculateRouteAsync_DrivingMode_ShouldCalculateFasterDurationThanWalking()
    {
        var origin = new GeoPoint(12.9716, 77.5946);
        var dest = new GeoPoint(12.9900, 77.6100);

        var walkingResult = await _navigationService.CalculateRouteAsync(origin, dest, "walking");
        var drivingResult = await _navigationService.CalculateRouteAsync(origin, dest, "driving");

        drivingResult.TotalDurationSeconds.Should().BeLessThan(walkingResult.TotalDurationSeconds);
    }
}
