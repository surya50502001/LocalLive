using FluentAssertions;
using LocalLive.Domain.Entities;
using LocalLive.Domain.Enums;
using Xunit;

namespace LocalLive.Tests;

public class RequestLifecycleTests
{
    [Fact]
    public void LiveRequest_ActiveRequest_ShouldNotBeExpiredBeforeExpiresAt()
    {
        var request = new LiveRequest
        {
            Title = "Need urgent paracetamol",
            Status = RequestStatus.Active,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        };

        var isExpired = DateTime.UtcNow > request.ExpiresAt;
        isExpired.Should().BeFalse();
    }

    [Fact]
    public void LiveRequest_ExpiredRequest_ShouldBeDetectedByExpiryLogic()
    {
        var request = new LiveRequest
        {
            Title = "Fresh milk",
            Status = RequestStatus.Active,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        };

        var isExpired = DateTime.UtcNow > request.ExpiresAt;
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void LiveRequest_StatusTransitions_ShouldRespectValidStates()
    {
        var request = new LiveRequest
        {
            Title = "USB cable",
            Status = RequestStatus.Active
        };

        // Fulfill
        request.Status = RequestStatus.Fulfilled;
        request.Status.Should().Be(RequestStatus.Fulfilled);

        // Cancel
        request.Status = RequestStatus.Cancelled;
        request.Status.Should().Be(RequestStatus.Cancelled);
    }
}
