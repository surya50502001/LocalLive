using System.ComponentModel.DataAnnotations;

namespace LocalLive.Application.Features.Auth;

public record RegisterRequest
{
    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(8), MaxLength(128)]
    public string Password { get; init; } = string.Empty;

    [Required, MinLength(2), MaxLength(120)]
    public string FullName { get; init; } = string.Empty;

    [Phone, MaxLength(30)]
    public string? Phone { get; init; }

    public string? RegisterAs { get; init; }
}

public record LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public record RefreshRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}

public record UserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string Role { get; init; } = string.Empty;
    public bool IsVerified { get; init; }
}

public record TokenPairDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; init; }
    public DateTime RefreshTokenExpiresAt { get; init; }
}

public record AuthResultDto
{
    public UserDto User { get; init; } = null!;
    public TokenPairDto Tokens { get; init; } = null!;
}

public record ForgotPasswordRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;
}

public record ResetPasswordRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Token { get; init; } = string.Empty;

    [Required, MinLength(8), MaxLength(128)]
    public string NewPassword { get; init; } = string.Empty;
}

public record UpdateProfileRequest
{
    [Required, MinLength(2), MaxLength(120)]
    public string FullName { get; init; } = string.Empty;

    [Phone, MaxLength(30)]
    public string? Phone { get; init; }
}
