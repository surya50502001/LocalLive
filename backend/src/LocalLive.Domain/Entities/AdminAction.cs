using LocalLive.Domain.Common;
using LocalLive.Domain.Enums;

namespace LocalLive.Domain.Entities;

public class AdminAction : BaseEntity
{
    public Guid AdminUserId { get; set; }
    public User? AdminUser { get; set; }

    public AdminActionTarget TargetType { get; set; }
    public Guid? TargetId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? DetailJson { get; set; }
}
