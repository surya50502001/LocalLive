using LocalLive.Domain.Common;
using LocalLive.Domain.Entities;

namespace LocalLive.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ShopCategory> ShopCategories { get; set; } = new List<ShopCategory>();
}
