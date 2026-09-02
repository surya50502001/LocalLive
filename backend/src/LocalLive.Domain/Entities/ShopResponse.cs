using LocalLive.Domain.Common;
using LocalLive.Domain.Entities;

namespace LocalLive.Domain.Entities;

public class ShopResponse : BaseEntity
{
    public Guid RequestId { get; set; }
    public LiveRequest? Request { get; set; }

    public Guid ShopId { get; set; }
    public Shop? Shop { get; set; }

    public Guid ShopRequestId { get; set; }
    public ShopRequest? ShopRequest { get; set; }

    public Guid ResponderUserId { get; set; }
    public User? ResponderUser { get; set; }

    public string? Message { get; set; }
    public double DistanceM { get; set; }
}
