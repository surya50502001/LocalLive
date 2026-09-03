using LocalLive.Domain.Common;

namespace LocalLive.Domain.Entities;

public class FavoriteShop : BaseEntity
{
    public Guid CustomerUserId { get; set; }
    public User? CustomerUser { get; set; }

    public Guid ShopId { get; set; }
    public Shop? Shop { get; set; }
}
