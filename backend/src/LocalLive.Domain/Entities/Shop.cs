using LocalLive.Domain.Common;
using LocalLive.Domain.Entities;
using LocalLive.Domain.Enums;

namespace LocalLive.Domain.Entities;

public class Shop : BaseEntity
{
    public Guid OwnerUserId { get; set; }
    public User? OwnerUser { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? ImageUrl { get; set; }

    public bool IsOpen { get; set; }
    public ShopStatus Status { get; set; } = ShopStatus.Pending;

    public HoursOfOperation? Hours { get; set; }

    public ICollection<ShopCategory> ShopCategories { get; set; } = new List<ShopCategory>();
    public ICollection<ShopRequest> ShopRequests { get; set; } = new List<ShopRequest>();
    public ICollection<ShopResponse> ShopResponses { get; set; } = new List<ShopResponse>();
}
