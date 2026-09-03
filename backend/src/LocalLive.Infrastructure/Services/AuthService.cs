using System.Security.Cryptography;
using System.Text;
using LocalLive.Application.Common;
using LocalLive.Application.Common.Interfaces;
using LocalLive.Application.Features.Auth;
using LocalLive.Domain.Entities;
using LocalLive.Domain.Enums;
using LocalLive.Infrastructure.Auth;
using LocalLive.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LocalLive.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IJwtTokenService _jwt;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AppDbContext db,
        IPasswordHasherService passwordHasher,
        IJwtTokenService jwt,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwt = jwt;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<Result<AuthResultDto>> RegisterAsync(RegisterRequest request, string? ipAddress, string? deviceInfo)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var existing = await _db.Users.IgnoreQueryFilters()
            .AnyAsync(u => u.Email == email);
        if (existing)
        {
            return Result<AuthResultDto>.Failure(new Error(
                ErrorType.Conflict, "EMAIL_IN_USE", "An account with this email already exists."));
        }

        var role = request.RegisterAs?.ToLowerInvariant() switch
        {
            "shop" or "shop_owner" => UserRole.ShopOwner,
            "admin" => UserRole.Admin,
            _ => UserRole.Customer
        };
        if (role == UserRole.Admin)
        {
            role = UserRole.Customer; // never allow self-registering as admin
        }

        var user = new User
        {
            Email = email,
            Phone = NormalizePhone(request.Phone),
            FullName = request.FullName.Trim(),
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Role = role,
            IsVerified = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var tokens = await IssueTokensAsync(user, ipAddress, deviceInfo);
        return Result<AuthResultDto>.Success(new AuthResultDto
        {
            User = MapUser(user),
            Tokens = tokens
        });
    }

    public async Task<Result<AuthResultDto>> LoginAsync(LoginRequest request, string? ipAddress, string? deviceInfo)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user is null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Result<AuthResultDto>.Failure(new Error(
                ErrorType.Unauthorized, "INVALID_CREDENTIALS", "Invalid email or password."));
        }

        if (user.IsBlocked)
        {
            return Result<AuthResultDto>.Failure(new Error(
                ErrorType.Forbidden, "ACCOUNT_BLOCKED", "This account has been blocked."));
        }

        var tokens = await IssueTokensAsync(user, ipAddress, deviceInfo);
        return Result<AuthResultDto>.Success(new AuthResultDto
        {
            User = MapUser(user),
            Tokens = tokens
        });
    }

    public async Task<Result<TokenPairDto>> RefreshAsync(string refreshToken, string? ipAddress, string? deviceInfo)
    {
        var tokenHash = HashToken(refreshToken);
        var stored = await _db.RefreshTokens.IgnoreQueryFilters()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (stored is null)
        {
            return Result<TokenPairDto>.Failure(new Error(
                ErrorType.Unauthorized, "INVALID_REFRESH_TOKEN", "Invalid refresh token."));
        }

        if (stored.IsRevoked)
        {
            return Result<TokenPairDto>.Failure(new Error(
                ErrorType.Unauthorized, "TOKEN_REVOKED", "This refresh token has been revoked."));
        }

        if (stored.ExpiresAt <= DateTime.UtcNow)
        {
            return Result<TokenPairDto>.Failure(new Error(
                ErrorType.Unauthorized, "TOKEN_EXPIRED", "This refresh token has expired."));
        }

        if (stored.User is null || stored.User.IsBlocked)
        {
            return Result<TokenPairDto>.Failure(new Error(
                ErrorType.Forbidden, "ACCOUNT_BLOCKED", "This account has been blocked."));
        }

        // Rotate: revoke the used token, issue a new one, link them (reuse detection).
        if (stored.ReplacedByTokenHash is not null)
        {
            return Result<TokenPairDto>.Failure(new Error(
                ErrorType.Unauthorized, "TOKEN_REUSE_DETECTED", "Refresh token reuse detected."));
        }

        stored.IsRevoked = true;
        stored.RevokedAt = DateTime.UtcNow;

        var access = _jwt.CreateAccessToken(stored.User);
        var newRefresh = GenerateRefreshToken();
        var newToken = new RefreshToken
        {
            UserId = stored.User.Id,
            TokenHash = HashToken(newRefresh),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress
        };
        stored.ReplacedByTokenId = newToken.Id;
        stored.ReplacedByTokenHash = HashToken(newRefresh);
        _db.RefreshTokens.Add(newToken);

        await _db.SaveChangesAsync();

        return Result<TokenPairDto>.Success(new TokenPairDto
        {
            AccessToken = access.token,
            AccessTokenExpiresAt = access.expiresAt,
            RefreshToken = newRefresh,
            RefreshTokenExpiresAt = newToken.ExpiresAt
        });
    }

    public async Task<Result> LogoutAsync(string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);
        var stored = await _db.RefreshTokens.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        if (stored is not null && !stored.IsRevoked)
        {
            stored.IsRevoked = true;
            stored.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        return Result.Success();
    }

    public async Task<Result<UserDto>> GetMeAsync(Guid userId)
    {
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return Result<UserDto>.Failure(new Error(ErrorType.NotFound, "USER_NOT_FOUND", "User not found."));
        }
        return Result<UserDto>.Success(MapUser(user));
    }

    public async Task<Result<string>> ForgotPasswordAsync(string email)
    {
        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLowerInvariant());

        // Always return success to prevent email enumeration attacks
        if (user is null)
        {
            return Result<string>.Success("If an account with this email exists, a password reset token has been generated.");
        }

        // Generate a 6-digit numeric reset token
        var random = new Random();
        var token = random.Next(100000, 999999).ToString();
        user.PasswordResetToken = token;
        user.PasswordResetExpiresAt = DateTime.UtcNow.AddMinutes(30);

        await _db.SaveChangesAsync();

        _logger.LogInformation("Password reset requested for {Email}. Token: {Token}", user.Email, token);

        return Result<string>.Success(token);
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == request.Email.Trim().ToLowerInvariant());

        if (user is null || user.PasswordResetToken != request.Token.Trim() || user.PasswordResetExpiresAt < DateTime.UtcNow)
        {
            return Result.Failure(new Error(ErrorType.Validation, "INVALID_TOKEN", "The reset token is invalid or has expired."));
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetExpiresAt = null;

        await _db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<UserDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return Result<UserDto>.Failure(new Error(ErrorType.NotFound, "USER_NOT_FOUND", "User not found."));
        }

        user.FullName = request.FullName.Trim();
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : NormalizePhone(request.Phone);

        await _db.SaveChangesAsync();
        return Result<UserDto>.Success(MapUser(user));
    }

    private async Task<TokenPairDto> IssueTokensAsync(User user, string? ipAddress, string? deviceInfo)
    {
        var access = _jwt.CreateAccessToken(user);
        var refreshValue = GenerateRefreshToken();
        var refreshEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(refreshValue),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress
        };
        _db.RefreshTokens.Add(refreshEntity);
        await _db.SaveChangesAsync();

        return new TokenPairDto
        {
            AccessToken = access.token,
            AccessTokenExpiresAt = access.expiresAt,
            RefreshToken = refreshValue,
            RefreshTokenExpiresAt = refreshEntity.ExpiresAt
        };
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private static string? NormalizePhone(string? phone) => string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

    private static UserDto MapUser(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FullName = user.FullName,
        Phone = user.Phone,
        Role = user.Role.ToString(),
        IsVerified = user.IsVerified
    };
}
