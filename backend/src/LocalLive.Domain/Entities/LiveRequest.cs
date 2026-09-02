using LocalLive.Domain.Common;
using LocalLive.Domain.Entities;
using LocalLive.Domain.Enums;

namespace LocalLive.Domain.Entities;

public class LiveRequest : BaseEntity
{
    public Guid CustomerUserId { get; set; }
    public User? CustomerUser { get; set; }

    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Active;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public ICollection<ShopRequest> ShopRequests { get; set; } = new List<ShopRequest>();
    public ICollection<ShopResponse> ShopResponses { get; set; } = new List<ShopResponse>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
