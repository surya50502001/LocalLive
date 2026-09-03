using LocalLive.Application.Common.Interfaces;
using LocalLive.Application.Features.Categories;
using LocalLive.Application.Features.Search;
using LocalLive.Application.Features.Shops;
using LocalLive.Domain.Common;
using LocalLive.Domain.Common.Services;
using LocalLive.Domain.Enums;
using LocalLive.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocalLive.Infrastructure.Services;

public class SearchService : ISearchService
{
    private readonly AppDbContext _db;
    private readonly IDistanceCalculator _distanceCalculator;

    public SearchService(AppDbContext db, IDistanceCalculator distanceCalculator)
    {
        _db = db;
        _distanceCalculator = distanceCalculator;
    }

    public async Task<SearchResultDto> SearchAsync(string query, double? latitude = null, double? longitude = null, double radiusKm = 15)
    {
        var cleanQuery = (query ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(cleanQuery))
        {
            return new SearchResultDto { Query = query ?? string.Empty };
        }

        // 1. Search matching categories
        var categories = await _db.Categories
            .Where(c => c.IsActive && (c.Name.ToLower().Contains(cleanQuery) || c.Slug.ToLower().Contains(cleanQuery)))
            .OrderBy(c => c.SortOrder)
            .Take(5)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Icon = c.Icon,
                SortOrder = c.SortOrder
            })
            .ToListAsync();

        // 2. Search matching shops (by name, description, address, or category name)
        var categoryIds = categories.Select(c => c.Id).ToList();

        var matchedShops = await _db.Shops
            .Include(s => s.ShopCategories).ThenInclude(sc => sc.Category)
            .Where(s => s.Status == ShopStatus.Verified && s.IsOpen)
            .Where(s =>
                s.Name.ToLower().Contains(cleanQuery) ||
                (s.Description != null && s.Description.ToLower().Contains(cleanQuery)) ||
                s.Address.ToLower().Contains(cleanQuery) ||
                s.ShopCategories.Any(sc => sc.Category != null && (sc.Category.Name.ToLower().Contains(cleanQuery) || categoryIds.Contains(sc.CategoryId))))
            .Take(30)
            .ToListAsync();

        var shopDtos = new List<ShopDto>();
        var userLocation = latitude.HasValue && longitude.HasValue ? new GeoPoint(latitude.Value, longitude.Value) : null;

        foreach (var s in matchedShops)
        {
            double? distanceM = null;
            if (userLocation != null)
            {
                distanceM = _distanceCalculator.DistanceMeters(userLocation, new GeoPoint(s.Latitude, s.Longitude));
                if (distanceM > radiusKm * 1000)
                {
                    continue; // Out of search radius
                }
            }

            shopDtos.Add(new ShopDto
            {
                Id = s.Id,
                OwnerUserId = s.OwnerUserId,
                Name = s.Name,
                Description = s.Description,
                Phone = s.Phone,
                Address = s.Address,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                ImageUrl = s.ImageUrl,
                IsOpen = s.IsOpen,
                IsOnline = s.IsOnline,
                Status = s.Status.ToString(),
                IsVerified = s.Status == ShopStatus.Verified,
                DistanceM = distanceM.HasValue ? Math.Round(distanceM.Value) : null,
                Categories = s.ShopCategories.Where(sc => sc.Category != null).Select(sc => new CategoryDtoRef
                {
                    Id = sc.Category!.Id,
                    Name = sc.Category.Name,
                    Slug = sc.Category.Slug
                }).ToList()
            });
        }

        if (userLocation != null)
        {
            shopDtos = shopDtos.OrderBy(s => s.DistanceM ?? double.MaxValue).ToList();
        }

        return new SearchResultDto
        {
            Query = query,
            Categories = categories,
            Shops = shopDtos
        };
    }
}
