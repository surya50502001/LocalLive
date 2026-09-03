using LocalLive.Application.Features.Categories;
using LocalLive.Application.Features.Shops;

namespace LocalLive.Application.Features.Search;

public class SearchResultDto
{
    public string Query { get; set; } = string.Empty;
    public List<CategoryDto> Categories { get; set; } = new();
    public List<ShopDto> Shops { get; set; } = new();
    public int TotalResults => Categories.Count + Shops.Count;
}
