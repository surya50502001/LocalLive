using LocalLive.Domain.Common;
using LocalLive.Domain.Entities;
using LocalLive.Domain.Enums;

namespace LocalLive.Domain.Entities;

public class ShopRequest : BaseEntity
{
    public Guid RequestId { get; set; }
    public LiveRequest? Request { get; set; }

    public Guid ShopId { get; set; }
    public Shop? Shop { get; set; }

    public ShopRequestStatus Status { get; set; } = ShopRequestStatus.Notified;
    public double DistanceM { get; set; }
    public DateTime? NotifiedAt { get; set; }
    public DateTime? RespondedAt { get; set; }

    public ShopResponse? Response { get; set; }
}
