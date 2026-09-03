using FluentAssertions;
using LocalLive.Infrastructure.Auth;
using Xunit;

namespace LocalLive.Tests;

public class PasswordHasherTests
{
    private readonly PasswordHasherService _hasher = new();

    [Fact]
    public void HashPassword_ShouldProduceNonEmptyHash()
    {
        var password = "SecurePassword123!";
        var hash = _hasher.HashPassword(password);

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe(password);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        var password = "MySecretPassword!";
        var hash = _hasher.HashPassword(password);

        var isValid = _hasher.VerifyPassword(password, hash);
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        var password = "CorrectPassword123!";
        var hash = _hasher.HashPassword(password);

        var isValid = _hasher.VerifyPassword("WrongPassword!", hash);
        isValid.Should().BeFalse();
    }

    [Fact]
    public void HashPassword_ShouldProduceUniqueSaltsForSamePassword()
    {
        var password = "IdenticalPassword123!";
        var hash1 = _hasher.HashPassword(password);
        var hash2 = _hasher.HashPassword(password);

        hash1.Should().NotBe(hash2);
    }
}
