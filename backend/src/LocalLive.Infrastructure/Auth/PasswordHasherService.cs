using LocalLive.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace LocalLive.Infrastructure.Auth;

public interface IPasswordHasherService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

/// <summary>
/// Wraps ASP.NET Core's PBKDF2-based PasswordHasher (v3, salted+iterated).
/// </summary>
public class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<User> _hasher = new();

    public string HashPassword(string password)
        => _hasher.HashPassword(null!, password);

    public bool VerifyPassword(string password, string hash)
        => _hasher.VerifyHashedPassword(null!, hash, password) != PasswordVerificationResult.Failed;
}
