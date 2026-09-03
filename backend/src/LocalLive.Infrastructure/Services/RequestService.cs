using LocalLive.Application.Common;
using LocalLive.Application.Common.Interfaces;
using LocalLive.Application.Features.Requests;
using LocalLive.Domain.Common;
using LocalLive.Domain.Common.Services;
using LocalLive.Domain.Entities;
using LocalLive.Domain.Enums;
using LocalLive.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocalLive.Infrastructure.Services;

public class RequestService : IRequestService
{
    private readonly AppDbContext _db;
    private readonly IDistanceCalculator _distance;
    private readonly INavigationProvider _navigation;
    private readonly IRealtimeNotifier _realtime;
    private readonly ILogger<RequestService> _logger;

    private const int DefaultTtlMinutes = 30;
    private const double DefaultSearchRadiusKm = 10;

    public RequestService(
        AppDbContext db,
        IDistanceCalculator distance,
        INavigationProvider navigation,
        IRealtimeNotifier realtime,
        ILogger<RequestService> logger)
    {
        _db = db;
        _distance = distance;
        _navigation = navigation;
        _realtime = realtime;
        _logger = logger;
    }

    public async Task<Result<RequestDto>> CreateAsync(Guid customerUserId, CreateRequestRequest request)
    {
        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.IsActive && c.DeletedAt == null);
        if (category is null)
        {
            return Result<RequestDto>.Failure(new Error(
                ErrorType.Validation, "INVALID_CATEGORY", "Selected category is not available.", "CategoryId"));
        }

        var origin = new GeoPoint(request.Latitude, request.Longitude);
        if (!origin.IsValid)
        {
            return Result<RequestDto>.Failure(new Error(
                ErrorType.Validation, "INVALID_LOCATION", "A valid location is required.", "Location"));
        }

        var ttl = request.TtlMinutes ?? DefaultTtlMinutes;
        if (ttl is < 5 or > 120)
        {
            return Result<RequestDto>.Failure(new Error(
                ErrorType.Validation, "INVALID_TTL", "Request duration must be between 5 and 120 minutes.", "TtlMinutes"));
        }

        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(ttl);

        var liveRequest = new LiveRequest
        {
            CustomerUserId = customerUserId,
            CategoryId = request.CategoryId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Status = RequestStatus.Active,
            ExpiresAt = expiresAt
        };
        _db.LiveRequests.Add(liveRequest);

        // 1. Find relevant nearby, verified, open shops.
        var radiusM = DefaultSearchRadiusKm * 1000;
        var candidateShops = await _db.Shops
            .AsNoTracking()
            .Include(s => s.ShopCategories)
            .Where(s => s.Status == ShopStatus.Verified)
            .Where(s => s.IsOpen && s.IsOnline)
            .Where(s => s.ShopCategories.Any(sc => sc.CategoryId == request.CategoryId))
            .ToListAsync();

        var matched = candidateShops
            .Select(s => new { Shop = s, Distance = _distance.DistanceMeters(origin, new GeoPoint(s.Latitude, s.Longitude)) })
            .Where(x => x.Distance <= radiusM)
            .OrderBy(x => x.Distance)
            .ToList();

        // 2. Record delivery/notification information for each matched shop.
        var shopRequests = matched.Select(x => new ShopRequest
        {
            Request = liveRequest,
            ShopId = x.Shop.Id,
            DistanceM = x.Distance,
            Status = ShopRequestStatus.Notified,
            NotifiedAt = DateTime.UtcNow
        }).ToList();

        _db.ShopRequests.AddRange(shopRequests);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created live request {RequestId} by customer {CustomerId}; notified {Count} shops",
            liveRequest.Id, customerUserId, shopRequests.Count);

        // 3. Broadcast to each matched shop in real time.
        foreach (var x in matched)
        {
            var payload = new
            {
                requestId = liveRequest.Id,
                title = liveRequest.Title,
                description = liveRequest.Description,
                categoryName = category.Name,
                categoryId = category.Id,
                customerNote = liveRequest.Description,
                distanceM = (int)x.Distance,
                distanceKm = Math.Round(x.Distance / 1000.0, 1),
                expiresAt,
                customerLatitude = liveRequest.Latitude,
                customerLongitude = liveRequest.Longitude,
                requestCreatedAt = liveRequest.CreatedAt
            };
            await _realtime.NotifyShopNewRequestAsync(x.Shop.Id, payload);
        }

        return Result<RequestDto>.Success(await MapToDtoAsync(liveRequest, viewerRole: "customer", viewerUserId: customerUserId, includePrivate: true));
    }

    public async Task<Result<RequestDto>> GetByIdAsync(Guid requestId, Guid? viewerUserId, string viewerRole)
    {
        var req = await LoadRequestAsync(requestId);
        if (req is null)
        {
            return Result<RequestDto>.Failure(new Error(ErrorType.NotFound, "REQUEST_NOT_FOUND", "Request not found."));
        }

        var isOwner = viewerUserId.HasValue && req.CustomerUserId == viewerUserId.Value;
        var isAdmin = viewerRole == nameof(UserRole.Admin);
        if (!isOwner && !isAdmin)
        {
            bool isAssignedShop = viewerUserId.HasValue && await _db.ShopRequests
                .AnyAsync(sr => sr.RequestId == req.Id && sr.Shop!.OwnerUserId == viewerUserId.Value);
            if (!isAssignedShop)
            {
                return Result<RequestDto>.Failure(new Error(
                    ErrorType.Forbidden, "FORBIDDEN", "You do not have access to this request."));
            }
        }

        return Result<RequestDto>.Success(await MapToDtoAsync(req, viewerRole, viewerUserId, includePrivate: isOwner || isAdmin));
    }

    public async Task<Result<RequestDto>> CancelAsync(Guid customerUserId, Guid requestId)
    {
        var req = await LoadRequestAsync(requestId);
        if (req is null)
        {
            return Result<RequestDto>.Failure(new Error(ErrorType.NotFound, "REQUEST_NOT_FOUND", "Request not found."));
        }
        if (req.CustomerUserId != customerUserId)
        {
            return Result<RequestDto>.Failure(new Error(
                ErrorType.Forbidden, "FORBIDDEN", "You can only cancel your own request."));
        }
        if (req.Status != RequestStatus.Active)
        {
            return Result<RequestDto>.Failure(new Error(
                ErrorType.Conflict, "REQUEST_NOT_ACTIVE", "Only active requests can be cancelled."));
        }

        req.Status = RequestStatus.Cancelled;
        req.ClosedAt = DateTime.UtcNow;

        var shopReqs = await _db.ShopRequests.Where(x => x.RequestId == req.Id && x.Status == ShopRequestStatus.Notified).ToListAsync();
        foreach (var sr in shopReqs)
        {
            sr.Status = ShopRequestStatus.Cancelled;
        }

        await _db.SaveChangesAsync();

        await _realtime.NotifyRequestStatusChangedAsync(customerUserId, req.Id, RequestStatus.Cancelled.ToString());
        await _realtime.NotifyShopRequestClosedAsync(req.Id);

        return Result<RequestDto>.Success(await MapToDtoAsync(req, "customer", customerUserId, true));
    }

    public async Task<Result<RequestDto>> FulfillAsync(Guid customerUserId, Guid requestId)
    {
        var req = await LoadRequestAsync(requestId);
        if (req is null)
        {
            return Result<RequestDto>.Failure(new Error(ErrorType.NotFound, "REQUEST_NOT_FOUND", "Request not found."));
        }
        if (req.CustomerUserId != customerUserId)
        {
            return Result<RequestDto>.Failure(new Error(
                ErrorType.Forbidden, "FORBIDDEN", "You can only fulfill your own request."));
        }
        if (req.Status != RequestStatus.Active)
        {
            return Result<RequestDto>.Failure(new Error(
                ErrorType.Conflict, "REQUEST_NOT_ACTIVE", "Only active requests can be fulfilled."));
        }

        req.Status = RequestStatus.Fulfilled;
        req.ClosedAt = DateTime.UtcNow;

        var shopReqs = await _db.ShopRequests.Where(x => x.RequestId == req.Id).ToListAsync();
        foreach (var sr in shopReqs.Where(x => x.Status == ShopRequestStatus.Notified))
        {
            sr.Status = ShopRequestStatus.Fulfilled;
        }

        await _db.SaveChangesAsync();

        await _realtime.NotifyRequestStatusChangedAsync(customerUserId, req.Id, RequestStatus.Fulfilled.ToString());
        await _realtime.NotifyShopRequestClosedAsync(req.Id);

        return Result<RequestDto>.Success(await MapToDtoAsync(req, "customer", customerUserId, true));
    }

    public async Task<Result<RequestDto>> AvailableAsync(Guid shopOwnerUserId, Guid requestId, string? message)
    {
        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.OwnerUserId == shopOwnerUserId && s.DeletedAt == null);
        if (shop is null)
        {
            return Result<RequestDto>.Failure(new Error(ErrorType.NotFound, "SHOP_NOT_FOUND", "Shop not found."));
        }

        var shopRequest = await _db.ShopRequests
            .FirstOrDefaultAsync(x => x.RequestId == requestId && x.ShopId == shop.Id);
        if (shopRequest is null)
        {
            return Result<RequestDto>.Failure(new Error(
                ErrorType.Forbidden, "NOT_NOTIFIED", "Your shop was not notified about this request."));
        }
        if (shop.Status != ShopStatus.Verified)
        {
            return Result<RequestDto>.Failure(new Error(
                ErrorType.Conflict, "SHOP_NOT_VERIFIED", "Your shop must be verified to respond to requests."));
        }
        if (!shop.IsOpen)
        {
            return Result<RequestDto>.Failure(new Error(
                ErrorType.Conflict, "SHOP_CLOSED", "Your shop is currently offline."));
        }

        var req = await LoadRequestAsync(requestId);
        if (req is null)
        {
            return Result<RequestDto>.Failure(new Error(ErrorType.NotFound, "REQUEST_NOT_FOUND", "Request not found."));
        }
        if (req.Status != RequestStatus.Active)
        {
            return Result<RequestDto>.Failure(new Error(
                ErrorType.Conflict, "REQUEST_NOT_ACTIVE", "This request is no longer active."));
        }

        var existingResponse = await _db.ShopResponses.AnyAsync(r => r.RequestId == req.Id && r.ShopId == shop.Id);
        if (existingResponse)
        {
            return Result<RequestDto>.Failure(new Error(
                ErrorType.Conflict, "ALREADY_RESPONDED", "Your shop has already responded to this request."));
        }

        var distance = _distance.DistanceMeters(
            new GeoPoint(req.Latitude, req.Longitude),
            new GeoPoint(shop.Latitude, shop.Longitude));

        var shopResponse = new ShopResponse
        {
            RequestId = req.Id,
            ShopId = shop.Id,
            ShopRequestId = shopRequest.Id,
            ResponderUserId = shopOwnerUserId,
            Message = message?.Trim(),
            DistanceM = distance
        };
        _db.ShopResponses.Add(shopResponse);

        shopRequest.Status = ShopRequestStatus.Responded;
        shopRequest.RespondedAt = DateTime.UtcNow;

        var notification = new Notification
        {
            RecipientUserId = req.CustomerUserId,
            Type = NotificationType.ShopAvailable,
            Title = "AVAILABLE NOW",
            Body = $"{shop.Name} — AVAILABLE NOW\n{FormatDistance(distance)} away",
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { requestId = req.Id, shopId = shop.Id, distanceM = Math.Round(distance) }),
            LinkedEntity = "ShopResponse",
            LinkedEntityId = shopResponse.Id
        };
        _db.Notifications.Add(notification);

        await _db.SaveChangesAsync();

        _logger.LogInformation("Shop {ShopId} marked request {RequestId} as AVAILABLE", shop.Id, req.Id);

        var payload = new
        {
            requestId = req.Id,
            requestTitle = req.Title,
            shopId = shop.Id,
            shopName = shop.Name,
            description = shop.Description,
            address = shop.Address,
            phone = shop.Phone,
            distanceM = (int)distance,
            distanceKm = Math.Round(distance / 1000.0, 1),
            distanceLabel = FormatDistance(distance),
            isVerified = true,
            respondedAt = DateTime.UtcNow,
            message = message?.Trim(),
            navigationUrl = BuildNavigationUrl(new GeoPoint(req.Latitude, req.Longitude), new GeoPoint(shop.Latitude, shop.Longitude))
        };
        await _realtime.NotifyCustomerShopAvailableAsync(req.CustomerUserId, payload);

        return Result<RequestDto>.Success(await MapToDtoAsync(req, "shop", shopOwnerUserId, false));
    }

    public async Task<List<RequestDto>> GetMyLiveRequestsAsync(Guid customerUserId)
    {
        var now = DateTime.UtcNow;
        var reqs = await _db.LiveRequests
            .Include(r => r.Category)
            .Include(r => r.ShopResponses).ThenInclude(sr => sr.Shop)
            .Where(r => r.CustomerUserId == customerUserId && r.Status == RequestStatus.Active && r.ExpiresAt > now)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var result = new List<RequestDto>();
        foreach (var r in reqs)
        {
            result.Add(await MapToDtoAsync(r, "customer", customerUserId, true));
        }
        return result;
    }

    public async Task<List<RequestDto>> GetShopLiveRequestsAsync(Guid shopOwnerUserId)
    {
        var shop = await _db.Shops.AsNoTracking().FirstOrDefaultAsync(s => s.OwnerUserId == shopOwnerUserId);
        if (shop is null)
        {
            return new List<RequestDto>();
        }

        var now = DateTime.UtcNow;
        var reqs = await _db.LiveRequests
            .Include(r => r.Category)
            .AsNoTracking()
            .Where(r => r.ShopRequests.Any(sr => sr.ShopId == shop.Id))
            .Where(r => r.Status == RequestStatus.Active && r.ExpiresAt > now)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var result = new List<RequestDto>();
        foreach (var r in reqs)
        {
            var shopReq = await _db.ShopRequests.AsNoTracking()
                .FirstAsync(x => x.RequestId == r.Id && x.ShopId == shop.Id);
            result.Add(new RequestDto
            {
                Id = r.Id,
                Title = r.Title,
                Description = r.Description,
                CategoryId = r.CategoryId,
                CategoryName = r.Category?.Name ?? string.Empty,
                Status = r.Status.ToString(),
                ExpiresAt = r.ExpiresAt,
                CreatedAt = r.CreatedAt,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                NotifiedShopsCount = await _db.ShopRequests.CountAsync(x => x.RequestId == r.Id),
                AvailableShops = new List<ShopAvailableDto>(),
                DistanceM = shopReq.DistanceM
            });
        }
        return result.OrderBy(x => x.DistanceM).ToList();
    }

    public async Task MarkExpiredRequestsAsync()
    {
        var now = DateTime.UtcNow;
        var expired = await _db.LiveRequests
            .Where(r => r.Status == RequestStatus.Active && r.ExpiresAt <= now)
            .ToListAsync();
        if (expired.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Expiring {Count} live requests", expired.Count);

        foreach (var r in expired)
        {
            r.Status = RequestStatus.Expired;
            r.ClosedAt = now;
        }

        var requestIds = expired.Select(r => r.Id).ToList();
        var shopReqs = await _db.ShopRequests
            .Where(x => requestIds.Contains(x.RequestId) && x.Status == ShopRequestStatus.Notified)
            .ToListAsync();
        foreach (var sr in shopReqs)
        {
            sr.Status = ShopRequestStatus.Expired;
        }

        await _db.SaveChangesAsync();

        foreach (var r in expired)
        {
            await _realtime.NotifyRequestStatusChangedAsync(r.CustomerUserId, r.Id, RequestStatus.Expired.ToString());
        }
        foreach (var id in requestIds)
        {
            await _realtime.NotifyShopRequestClosedAsync(id);
        }
    }

    private async Task<LiveRequest?> LoadRequestAsync(Guid requestId)
        => await _db.LiveRequests
            .Include(r => r.Category)
            .Include(r => r.ShopResponses).ThenInclude(sr => sr.Shop)
            .FirstOrDefaultAsync(r => r.Id == requestId);

    private async Task<RequestDto> MapToDtoAsync(LiveRequest req, string viewerRole, Guid? viewerUserId, bool includePrivate)
    {
        var customerOrigin = new GeoPoint(req.Latitude, req.Longitude);

        var availableShops = req.ShopResponses
            .Where(sr => sr.Shop != null)
            .OrderBy(sr => sr.DistanceM)
            .Select(sr => new ShopAvailableDto
            {
                ShopId = sr.Shop!.Id,
                ShopName = sr.Shop.Name,
                Description = sr.Shop.Description,
                Address = sr.Shop.Address,
                Phone = sr.Shop.Phone,
                Latitude = sr.Shop.Latitude,
                Longitude = sr.Shop.Longitude,
                DistanceM = sr.DistanceM,
                IsVerified = sr.Shop.Status == ShopStatus.Verified,
                Message = sr.Message,
                RespondedAt = sr.CreatedAt,
                NavigationUrl = BuildNavigationUrl(customerOrigin, new GeoPoint(sr.Shop.Latitude, sr.Shop.Longitude))
            }).ToList();

        var notifiedCount = await _db.ShopRequests.CountAsync(x => x.RequestId == req.Id);

        return new RequestDto
        {
            Id = req.Id,
            Title = req.Title,
            Description = req.Description,
            CategoryId = req.CategoryId,
            CategoryName = req.Category?.Name ?? string.Empty,
            Status = req.Status.ToString(),
            ExpiresAt = req.ExpiresAt,
            CreatedAt = req.CreatedAt,
            Latitude = req.Latitude,
            Longitude = req.Longitude,
            NotifiedShopsCount = notifiedCount,
            AvailableShops = includePrivate ? availableShops : new List<ShopAvailableDto>()
        };
    }

    private string? BuildNavigationUrl(GeoPoint from, GeoPoint to)
        => _navigation.BuildNavigationUrl(from, to, "shop");

    private static string FormatDistance(double meters) =>
        meters < 1000 ? $"{Math.Round(meters)}m" : $"{Math.Round(meters / 1000.0, 1)}km";
}
