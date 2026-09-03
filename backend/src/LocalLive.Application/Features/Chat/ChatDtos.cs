namespace LocalLive.Application.Features.Chat;

public class ConversationDto
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public string RequestTitle { get; set; } = string.Empty;
    public Guid CustomerUserId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public DateTime? LastMessageAt { get; set; }
    public string? LastMessageContent { get; set; }
    public int UnreadCount { get; set; }
    public bool IsClosed { get; set; }
}

public class ChatMessageDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderUserId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}

public class SendMessageRequest
{
    public string Content { get; set; } = string.Empty;
}
