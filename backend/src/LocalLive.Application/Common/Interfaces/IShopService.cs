using LocalLive.Application.Common;
using LocalLive.Application.Features.Categories;
using LocalLive.Application.Features.Shops;

namespace LocalLive.Application.Common.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetActiveAsync();
}

public interface IShopService
{
    Task<Result<ShopDto>> CreateAsync(Guid ownerUserId, CreateShopRequest request);
    Task<Result<ShopDto>> GetByIdAsync(Guid shopId);
    Task<Result<ShopDto>> GetMyShopAsync(Guid ownerUserId);
    Task<Result<ShopDto>> UpdateAsync(Guid ownerUserId, Guid shopId, UpdateShopRequest request);
    Task<Result<ShopDto>> UpdateStatusAsync(Guid ownerUserId, Guid shopId, bool isOpen);
    Task<List<ShopDto>> GetNearbyAsync(NearbyShopQuery query);
    Task<Result<Guid>> GetShopIdForOwnerAsync(Guid ownerUserId);
}
