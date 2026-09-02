using LocalLive.Domain.Common;
using LocalLive.Domain.Entities;

namespace LocalLive.Domain.Entities;

public class ShopCategory : BaseEntity
{
    public Guid ShopId { get; set; }
    public Shop? Shop { get; set; }

    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
}
