using LocalLive.Domain.Common;
using LocalLive.Domain.Entities;
using LocalLive.Domain.Enums;

namespace LocalLive.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid RecipientUserId { get; set; }
    public User? RecipientUser { get; set; }

    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? PayloadJson { get; set; }
    public string? LinkedEntity { get; set; }
    public Guid? LinkedEntityId { get; set; }

    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
