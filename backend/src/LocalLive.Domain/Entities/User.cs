using LocalLive.Domain.Common;
using LocalLive.Domain.Enums;

namespace LocalLive.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Customer;

    public bool IsVerified { get; set; }
    public bool IsBlocked { get; set; }
    public DateTime? BlockedAt { get; set; }
    public string? BlockReason { get; set; }

    public ICollection<Shop> OwnedShops { get; set; } = new List<Shop>();
    public ICollection<LiveRequest> Requests { get; set; } = new List<LiveRequest>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
