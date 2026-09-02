using LocalLive.Application.Common;
using LocalLive.Application.Features.Auth;

namespace LocalLive.Application.Common.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResultDto>> RegisterAsync(RegisterRequest request, string? ipAddress, string? deviceInfo);
    Task<Result<AuthResultDto>> LoginAsync(LoginRequest request, string? ipAddress, string? deviceInfo);
    Task<Result<TokenPairDto>> RefreshAsync(string refreshToken, string? ipAddress, string? deviceInfo);
    Task<Result> LogoutAsync(string refreshToken);
    Task<Result<UserDto>> GetMeAsync(Guid userId);
}
