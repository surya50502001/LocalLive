using LocalLive.Domain.Common;

namespace LocalLive.Domain.Entities;

public class UserBlock : BaseEntity
{
    public Guid BlockerUserId { get; set; }
    public User? BlockerUser { get; set; }

    public Guid BlockedUserId { get; set; }
    public User? BlockedUser { get; set; }

    public string? Reason { get; set; }
}
