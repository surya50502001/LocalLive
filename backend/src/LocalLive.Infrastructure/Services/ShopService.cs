using LocalLive.Application.Common;
using LocalLive.Application.Common.Interfaces;
using LocalLive.Application.Features.Shops;
using LocalLive.Domain.Common;
using LocalLive.Domain.Common.Services;
using LocalLive.Domain.Entities;
using LocalLive.Domain.Enums;
using LocalLive.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocalLive.Infrastructure.Services;

public class ShopService : IShopService
{
    private readonly AppDbContext _db;
    private readonly IDistanceCalculator _distance;
    private readonly INavigationProvider _navigation;

    public ShopService(AppDbContext db, IDistanceCalculator distance, INavigationProvider navigation)
    {
        _db = db;
        _distance = distance;
        _navigation = navigation;
    }

    public async Task<Result<ShopDto>> CreateAsync(Guid ownerUserId, CreateShopRequest request)
    {
        var hasShop = await _db.Shops.AnyAsync(s => s.OwnerUserId == ownerUserId);
        if (hasShop)
        {
            return Result<ShopDto>.Failure(new Error(
                ErrorType.Conflict, "SHOP_EXISTS", "You already have a registered shop."));
        }

        var validCategories = await GetValidCategoriesAsync(request.CategoryIds);
        if (validCategories.Count != request.CategoryIds.Count)
        {
            return Result<ShopDto>.Failure(new Error(
                ErrorType.Validation, "INVALID_CATEGORY", "One or more categories are invalid or inactive.",
                "CategoryIds"));
        }

        var shop = new Shop
        {
            OwnerUserId = ownerUserId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Phone = request.Phone.Trim(),
            Address = request.Address.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            ImageUrl = request.ImageUrl,
            Hours = request.Hours,
            Status = ShopStatus.Verified,
            IsOpen = true
        };

        foreach (var c in validCategories)
        {
            shop.ShopCategories.Add(new ShopCategory { Shop = shop, Category = c });
        }

        _db.Shops.Add(shop);
        await _db.SaveChangesAsync();

        return Result<ShopDto>.Success(MapToDtoAsync(shop, null));
    }

    public async Task<Result<ShopDto>> GetByIdAsync(Guid shopId)
    {
        var shop = await LoadShopAsync(shopId);
        if (shop is null)
        {
            return Result<ShopDto>.Failure(new Error(ErrorType.NotFound, "SHOP_NOT_FOUND", "Shop not found."));
        }
        return Result<ShopDto>.Success(MapToDtoAsync(shop, null));
    }

    public async Task<Result<ShopDto>> GetMyShopAsync(Guid ownerUserId)
    {
        var shop = await LoadShopByOwnerAsync(ownerUserId);
        if (shop is null)
        {
            return Result<ShopDto>.Failure(new Error(ErrorType.NotFound, "SHOP_NOT_FOUND", "Shop not found."));
        }
        return Result<ShopDto>.Success(MapToDtoAsync(shop, null));
    }

    public async Task<Result<Guid>> GetShopIdForOwnerAsync(Guid ownerUserId)
    {
        var shop = await _db.Shops.AsNoTracking()
            .Where(s => s.OwnerUserId == ownerUserId && s.DeletedAt == null)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync();
        return shop is null
            ? Result<Guid>.Failure(new Error(ErrorType.NotFound, "SHOP_NOT_FOUND", "Shop not found."))
            : Result<Guid>.Success(shop.Value);
    }

    public async Task<Result<ShopDto>> UpdateAsync(Guid ownerUserId, Guid shopId, UpdateShopRequest request)
    {
        var shop = await LoadShopAsync(shopId);
        if (shop is null)
        {
            return Result<ShopDto>.Failure(new Error(ErrorType.NotFound, "SHOP_NOT_FOUND", "Shop not found."));
        }
        if (shop.OwnerUserId != ownerUserId)
        {
            return Result<ShopDto>.Failure(new Error(
                ErrorType.Forbidden, "FORBIDDEN", "You can only modify your own shop."));
        }

        var validCategories = await GetValidCategoriesAsync(request.CategoryIds);
        if (validCategories.Count != request.CategoryIds.Count)
        {
            return Result<ShopDto>.Failure(new Error(
                ErrorType.Validation, "INVALID_CATEGORY", "One or more categories are invalid or inactive.",
                "CategoryIds"));
        }

        shop.Name = request.Name.Trim();
        shop.Description = request.Description?.Trim();
        shop.Phone = request.Phone.Trim();
        shop.Address = request.Address.Trim();
        shop.Latitude = request.Latitude;
        shop.Longitude = request.Longitude;
        shop.ImageUrl = request.ImageUrl;
        shop.Hours = request.Hours;

        var keep = new HashSet<Guid>(request.CategoryIds);
        foreach (var sc in shop.ShopCategories.Where(x => !keep.Contains(x.CategoryId)).ToList())
        {
            _db.Remove(sc);
        }
        var existing = shop.ShopCategories.Select(x => x.CategoryId).ToHashSet();
        foreach (var c in validCategories.Where(x => !existing.Contains(x.Id)))
        {
            _db.Add(new ShopCategory { ShopId = shop.Id, CategoryId = c.Id });
        }

        shop.MarkUpdated();
        await _db.SaveChangesAsync();

        return Result<ShopDto>.Success(MapToDtoAsync(shop, null));
    }

    public async Task<Result<ShopDto>> UpdateStatusAsync(Guid ownerUserId, Guid shopId, bool isOpen)
    {
        var shop = await LoadShopAsync(shopId);
        if (shop is null)
        {
            return Result<ShopDto>.Failure(new Error(ErrorType.NotFound, "SHOP_NOT_FOUND", "Shop not found."));
        }
        if (shop.OwnerUserId != ownerUserId)
        {
            return Result<ShopDto>.Failure(new Error(
                ErrorType.Forbidden, "FORBIDDEN", "You can only update your own shop's status."));
        }

        shop.IsOpen = isOpen;
        shop.MarkUpdated();
        await _db.SaveChangesAsync();

        return Result<ShopDto>.Success(MapToDtoAsync(shop, null));
    }

    public async Task<Result<ShopDto>> UpdateOnlineStatusAsync(Guid ownerUserId, Guid shopId, bool isOnline)
    {
        var shop = await LoadShopAsync(shopId);
        if (shop is null)
        {
            return Result<ShopDto>.Failure(new Error(ErrorType.NotFound, "SHOP_NOT_FOUND", "Shop not found."));
        }
        if (shop.OwnerUserId != ownerUserId)
        {
            return Result<ShopDto>.Failure(new Error(
                ErrorType.Forbidden, "FORBIDDEN", "You can only update your own shop's status."));
        }

        shop.IsOnline = isOnline;
        shop.MarkUpdated();
        await _db.SaveChangesAsync();

        return Result<ShopDto>.Success(MapToDtoAsync(shop, null));
    }

    public async Task<List<ShopDto>> GetNearbyAsync(NearbyShopQuery query)
    {
        var origin = new GeoPoint(query.Latitude, query.Longitude);
        if (!origin.IsValid)
        {
            return new List<ShopDto>();
        }

        var radiusM = query.RadiusKm * 1000;

        var shops = await _db.Shops
            .AsNoTracking()
            .Include(s => s.ShopCategories).ThenInclude(sc => sc.Category)
            .Where(s => s.Status == ShopStatus.Verified)
            .Where(s => s.IsOpen)
            .Where(s => query.CategoryId == null || s.ShopCategories.Any(sc => sc.CategoryId == query.CategoryId))
            .ToListAsync();

        var result = shops
            .Select(s => new
            {
                Shop = s,
                Distance = _distance.DistanceMeters(origin, new GeoPoint(s.Latitude, s.Longitude))
            })
            .Where(x => x.Distance <= radiusM)
            .OrderBy(x => x.Distance)
            .Take(50)
            .ToList();

        return result.Select(x => MapShop(x.Shop, null, x.Distance)).ToList();
    }

    private async Task<Shop?> LoadShopAsync(Guid shopId)
        => await _db.Shops
            .Include(s => s.ShopCategories).ThenInclude(sc => sc.Category)
            .FirstOrDefaultAsync(s => s.Id == shopId);

    private async Task<Shop?> LoadShopByOwnerAsync(Guid ownerUserId)
        => await _db.Shops
            .Include(s => s.ShopCategories).ThenInclude(sc => sc.Category)
            .FirstOrDefaultAsync(s => s.OwnerUserId == ownerUserId);

    private async Task<List<Category>> GetValidCategoriesAsync(List<Guid> ids)
        => await _db.Categories
            .Where(c => ids.Contains(c.Id) && c.IsActive && c.DeletedAt == null)
            .ToListAsync();

    private ShopDto MapToDtoAsync(Shop shop, double? distance)
        => MapShop(shop, _navigation, distance);

    private static ShopDto MapShop(Shop shop, INavigationProvider? navigation, double? distance)
    {
        var destination = new GeoPoint(shop.Latitude, shop.Longitude);
        return new ShopDto
        {
            Id = shop.Id,
            Name = shop.Name,
            Description = shop.Description,
            Phone = shop.Phone,
            Address = shop.Address,
            Latitude = shop.Latitude,
            Longitude = shop.Longitude,
            ImageUrl = shop.ImageUrl,
            IsOpen = shop.IsOpen,
            IsOnline = shop.IsOnline,
            Status = shop.Status.ToString(),
            IsVerified = shop.Status == ShopStatus.Verified,
            OwnerUserId = shop.OwnerUserId,
            Categories = shop.ShopCategories
                .Where(sc => sc.Category != null)
                .Select(sc => new CategoryDtoRef
                {
                    Id = sc.Category!.Id,
                    Name = sc.Category.Name,
                    Slug = sc.Category.Slug
                }).ToList(),
            DistanceM = distance,
            NavigationUrl = navigation is not null && distance.HasValue
                ? navigation.BuildNavigationUrl(new GeoPoint(0, 0), destination, shop.Name)
                : null
        };
    }

    public async Task<Result<bool>> ToggleFavoriteAsync(Guid customerUserId, Guid shopId)
    {
        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Id == shopId);
        if (shop is null)
        {
            return Result<bool>.Failure(new Error(ErrorType.NotFound, "SHOP_NOT_FOUND", "Shop not found."));
        }

        var existing = await _db.FavoriteShops
            .FirstOrDefaultAsync(f => f.CustomerUserId == customerUserId && f.ShopId == shopId);

        if (existing is not null)
        {
            _db.FavoriteShops.Remove(existing);
            await _db.SaveChangesAsync();
            return Result<bool>.Success(false); // Removed from favorites
        }

        _db.FavoriteShops.Add(new FavoriteShop
        {
            CustomerUserId = customerUserId,
            ShopId = shopId
        });
        await _db.SaveChangesAsync();
        return Result<bool>.Success(true); // Added to favorites
    }

    public async Task<List<ShopDto>> GetFavoriteShopsAsync(Guid customerUserId)
    {
        var shops = await _db.FavoriteShops
            .Include(f => f.Shop).ThenInclude(s => s!.ShopCategories).ThenInclude(sc => sc.Category)
            .Where(f => f.CustomerUserId == customerUserId && f.Shop != null)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => f.Shop!)
            .ToListAsync();

        return shops.Select(s => MapToDtoAsync(s, null)).ToList();
    }
}
