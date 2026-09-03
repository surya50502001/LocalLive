using LocalLive.Domain.Common;

namespace LocalLive.Domain.Entities;

public class Conversation : BaseEntity
{
    public Guid RequestId { get; set; }
    public LiveRequest? Request { get; set; }

    public Guid CustomerUserId { get; set; }
    public User? CustomerUser { get; set; }

    public Guid ShopId { get; set; }
    public Shop? Shop { get; set; }

    public DateTime? LastMessageAt { get; set; }
    public bool IsClosed { get; set; }

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
