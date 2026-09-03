using LocalLive.Domain.Common;

namespace LocalLive.Domain.Entities;

public class ChatMessage : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Conversation? Conversation { get; set; }

    public Guid SenderUserId { get; set; }
    public User? SenderUser { get; set; }

    public string Content { get; set; } = string.Empty;

    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
