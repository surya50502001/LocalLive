using LocalLive.Application.Common;
using LocalLive.Application.Common.Interfaces;
using LocalLive.Application.Features.Admin;
using LocalLive.Domain.Entities;
using LocalLive.Domain.Enums;
using LocalLive.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocalLive.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _db;

    public AdminService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AdminStatsDto> GetStatsAsync()
    {
        var totalUsers = await _db.Users.IgnoreQueryFilters().CountAsync();
        var totalCustomers = await _db.Users.IgnoreQueryFilters().CountAsync(u => u.Role == UserRole.Customer);
        var totalShopOwners = await _db.Users.IgnoreQueryFilters().CountAsync(u => u.Role == UserRole.ShopOwner);
        var totalShops = await _db.Shops.IgnoreQueryFilters().CountAsync();
        var verifiedShops = await _db.Shops.IgnoreQueryFilters().CountAsync(s => s.Status == ShopStatus.Verified);
        var pendingShops = await _db.Shops.IgnoreQueryFilters().CountAsync(s => s.Status == ShopStatus.Pending);
        var disabledShops = await _db.Shops.IgnoreQueryFilters().CountAsync(s => s.Status == ShopStatus.Disabled);
        var activeShopsNow = await _db.Shops.IgnoreQueryFilters().CountAsync(s => s.IsOpen && s.Status == ShopStatus.Verified);
        var totalRequests = await _db.LiveRequests.IgnoreQueryFilters().CountAsync();
        var activeRequestsNow = await _db.LiveRequests.IgnoreQueryFilters()
            .CountAsync(r => r.Status == RequestStatus.Active && r.ExpiresAt > DateTime.UtcNow);
        var fulfilled = await _db.LiveRequests.IgnoreQueryFilters().CountAsync(r => r.Status == RequestStatus.Fulfilled);
        var cancelled = await _db.LiveRequests.IgnoreQueryFilters().CountAsync(r => r.Status == RequestStatus.Cancelled);
        var expired = await _db.LiveRequests.IgnoreQueryFilters().CountAsync(r => r.Status == RequestStatus.Expired);
        var totalResponses = await _db.ShopResponses.IgnoreQueryFilters().CountAsync();
        var avgDistance = await _db.ShopResponses.IgnoreQueryFilters().AverageAsync(sr => (double?)sr.DistanceM) ?? 0;
        var openReports = await _db.Reports.IgnoreQueryFilters().CountAsync(r => r.Status == ReportStatus.Open);

        var requestsByCategory = await _db.LiveRequests.IgnoreQueryFilters()
            .GroupBy(r => r.Category!.Name)
            .Select(g => new CategoryStatDto { CategoryName = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        var daily = new List<DailyStatDto>();
        for (int i = 6; i >= 0; i--)
        {
            var day = DateTime.UtcNow.Date.AddDays(-i);
            var next = day.AddDays(1);
            var reqCount = await _db.LiveRequests.IgnoreQueryFilters().CountAsync(r => r.CreatedAt >= day && r.CreatedAt < next);
            var respCount = await _db.ShopResponses.IgnoreQueryFilters().CountAsync(r => r.CreatedAt >= day && r.CreatedAt < next);
            var fulfilledCount = await _db.LiveRequests.IgnoreQueryFilters().CountAsync(r => r.Status == RequestStatus.Fulfilled && r.CreatedAt >= day && r.CreatedAt < next);
            daily.Add(new DailyStatDto
            {
                Day = day.ToString("yyyy-MM-dd"),
                Requests = reqCount,
                Responses = respCount,
                Fulfilled = fulfilledCount
            });
        }

        return new AdminStatsDto
        {
            TotalUsers = totalUsers,
            TotalCustomers = totalCustomers,
            TotalShopOwners = totalShopOwners,
            TotalShops = totalShops,
            VerifiedShops = verifiedShops,
            PendingShops = pendingShops,
            DisabledShops = disabledShops,
            ActiveShopsNow = activeShopsNow,
            TotalRequests = totalRequests,
            ActiveRequestsNow = activeRequestsNow,
            FulfilledRequests = fulfilled,
            CancelledRequests = cancelled,
            ExpiredRequests = expired,
            TotalResponses = totalResponses,
            AvgDistanceToRespondingShopM = Math.Round(avgDistance),
            OpenReports = openReports,
            RequestsByCategory = requestsByCategory,
            RequestsLast7Days = daily
        };
    }

    public async Task<PaginatedResult<AdminShopDto>> GetShopsAsync(int page, int pageSize, string? status, string? search)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Shops.IgnoreQueryFilters().AsNoTracking()
            .Include(s => s.OwnerUser)
            .Include(s => s.ShopCategories).ThenInclude(sc => sc.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var parsed = Enum.TryParse<ShopStatus>(status, true, out var s);
            if (parsed)
            {
                query = query.Where(x => x.Status == s);
            }
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Name.Contains(term) || x.OwnerUser!.Email.Contains(term));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminShopDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Phone = x.Phone,
                Address = x.Address,
                IsOpen = x.IsOpen,
                Status = x.Status.ToString(),
                OwnerUserId = x.OwnerUserId,
                OwnerName = x.OwnerUser!.FullName,
                OwnerEmail = x.OwnerUser!.Email,
                Categories = x.ShopCategories.Select(sc => sc.Category!.Name).ToList(),
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return new PaginatedResult<AdminShopDto> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<Result> VerifyShopAsync(Guid adminUserId, Guid shopId)
        => await SetShopStatusAsync(adminUserId, shopId, ShopStatus.Verified, "verified_shop");

    public async Task<Result> DisableShopAsync(Guid adminUserId, Guid shopId)
        => await SetShopStatusAsync(adminUserId, shopId, ShopStatus.Disabled, "disabled_shop");

    public async Task<Result> EnableShopAsync(Guid adminUserId, Guid shopId)
        => await SetShopStatusAsync(adminUserId, shopId, ShopStatus.Verified, "enabled_shop");

    private async Task<Result> SetShopStatusAsync(Guid adminUserId, Guid shopId, ShopStatus status, string action)
    {
        var shop = await _db.Shops.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == shopId);
        if (shop is null)
        {
            return Result.Failure(new Error(ErrorType.NotFound, "SHOP_NOT_FOUND", "Shop not found."));
        }

        shop.Status = status;
        shop.MarkUpdated();
        await LogActionAsync(adminUserId, AdminActionTarget.Shop, shopId, action);
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<PaginatedResult<AdminUserDto>> GetUsersAsync(int page, int pageSize, string? search)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Users.IgnoreQueryFilters().AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u => u.Email.Contains(term) || u.FullName.Contains(term));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                Phone = u.Phone,
                Role = u.Role.ToString(),
                IsBlocked = u.IsBlocked,
                IsVerified = u.IsVerified,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return new PaginatedResult<AdminUserDto> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<Result> BlockUserAsync(Guid adminUserId, Guid userId, string? reason)
    {
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return Result.Failure(new Error(ErrorType.NotFound, "USER_NOT_FOUND", "User not found."));
        }
        if (user.Role == UserRole.Admin)
        {
            return Result.Failure(new Error(ErrorType.Forbidden, "CANNOT_BLOCK_ADMIN", "Admins cannot block other admins."));
        }

        user.IsBlocked = true;
        user.BlockedAt = DateTime.UtcNow;
        user.BlockReason = reason;
        await LogActionAsync(adminUserId, AdminActionTarget.User, userId, "blocked_user", reason);
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UnblockUserAsync(Guid adminUserId, Guid userId)
    {
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return Result.Failure(new Error(ErrorType.NotFound, "USER_NOT_FOUND", "User not found."));
        }
        user.IsBlocked = false;
        user.BlockedAt = null;
        user.BlockReason = null;
        await LogActionAsync(adminUserId, AdminActionTarget.User, userId, "unblocked_user");
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<PaginatedResult<AdminRequestDto>> GetRequestsAsync(int page, int pageSize, string? status)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.LiveRequests.IgnoreQueryFilters().AsNoTracking()
            .Include(r => r.CustomerUser)
            .Include(r => r.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var parsed = Enum.TryParse<RequestStatus>(status, true, out var s);
            if (parsed)
            {
                query = query.Where(x => x.Status == s);
            }
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new AdminRequestDto
            {
                Id = r.Id,
                Title = r.Title,
                CategoryName = r.Category!.Name,
                Status = r.Status.ToString(),
                CustomerId = r.CustomerUserId,
                CustomerName = r.CustomerUser!.FullName,
                CreatedAt = r.CreatedAt,
                ExpiresAt = r.ExpiresAt,
                NotifiedShops = r.ShopRequests.Count,
                ResponseCount = r.ShopResponses.Count
            })
            .ToListAsync();

        return new PaginatedResult<AdminRequestDto> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<PaginatedResult<AdminReportDto>> GetReportsAsync(int page, int pageSize, string? status)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Reports.IgnoreQueryFilters().AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            var parsed = Enum.TryParse<ReportStatus>(status, true, out var s);
            if (parsed)
            {
                query = query.Where(x => x.Status == s);
            }
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new AdminReportDto
            {
                Id = r.Id,
                ReporterUserId = r.ReporterUserId,
                TargetType = r.TargetType.ToString(),
                TargetId = r.TargetId,
                Reason = r.Reason,
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return new PaginatedResult<AdminReportDto> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<Result> ResolveReportAsync(Guid adminUserId, Guid reportId)
        => await SetReportStatusAsync(adminUserId, reportId, ReportStatus.Resolved, "resolved_report");

    public async Task<Result> DismissReportAsync(Guid adminUserId, Guid reportId)
        => await SetReportStatusAsync(adminUserId, reportId, ReportStatus.Dismissed, "dismissed_report");

    private async Task<Result> SetReportStatusAsync(Guid adminUserId, Guid reportId, ReportStatus status, string action)
    {
        var report = await _db.Reports.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == reportId);
        if (report is null)
        {
            return Result.Failure(new Error(ErrorType.NotFound, "REPORT_NOT_FOUND", "Report not found."));
        }
        report.Status = status;
        report.ResolvedByUserId = adminUserId;
        report.ResolvedAt = DateTime.UtcNow;
        await LogActionAsync(adminUserId, AdminActionTarget.Report, reportId, action);
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<List<AdminCategoryDto>> ListCategoriesAsync()
        => await _db.Categories.IgnoreQueryFilters().AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .Select(c => new AdminCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Icon = c.Icon,
                SortOrder = c.SortOrder,
                IsActive = c.IsActive
            })
            .ToListAsync();

    public async Task<Result<AdminCategoryDto>> CreateCategoryAsync(Guid adminUserId, UpsertCategoryRequest request)
    {
        var slug = BuildSlug(request.Name);
        var exists = await _db.Categories.IgnoreQueryFilters().AnyAsync(c => c.Slug == slug);
        if (exists)
        {
            return Result<AdminCategoryDto>.Failure(new Error(
                ErrorType.Conflict, "CATEGORY_EXISTS", "A category with this name already exists."));
        }

        var category = new Category
        {
            Name = request.Name.Trim(),
            Slug = slug,
            Icon = request.Icon,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive
        };
        _db.Categories.Add(category);
        await LogActionAsync(adminUserId, AdminActionTarget.Category, category.Id, "created_category");
        await _db.SaveChangesAsync();

        return Result<AdminCategoryDto>.Success(MapCategory(category));
    }

    public async Task<Result<AdminCategoryDto>> UpdateCategoryAsync(Guid adminUserId, Guid id, UpsertCategoryRequest request)
    {
        var category = await _db.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
        if (category is null)
        {
            return Result<AdminCategoryDto>.Failure(new Error(ErrorType.NotFound, "CATEGORY_NOT_FOUND", "Category not found."));
        }

        category.Name = request.Name.Trim();
        category.Slug = string.IsNullOrWhiteSpace(request.Slug) ? BuildSlug(request.Name) : request.Slug;
        category.Icon = request.Icon;
        category.SortOrder = request.SortOrder;
        category.IsActive = request.IsActive;
        category.MarkUpdated();
        await LogActionAsync(adminUserId, AdminActionTarget.Category, id, "updated_category");
        await _db.SaveChangesAsync();

        return Result<AdminCategoryDto>.Success(MapCategory(category));
    }

    public async Task<Result> DeleteCategoryAsync(Guid adminUserId, Guid id)
    {
        var category = await _db.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
        if (category is null)
        {
            return Result.Failure(new Error(ErrorType.NotFound, "CATEGORY_NOT_FOUND", "Category not found."));
        }
        category.SoftDelete();
        await LogActionAsync(adminUserId, AdminActionTarget.Category, id, "deleted_category");
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    private Task LogActionAsync(Guid adminUserId, AdminActionTarget target, Guid? targetId, string action, string? detail = null)
    {
        _db.AdminActions.Add(new AdminAction
        {
            AdminUserId = adminUserId,
            TargetType = target,
            TargetId = targetId,
            Action = action,
            DetailJson = detail is null ? null : System.Text.Json.JsonSerializer.Serialize(new { note = detail })
        });
        return Task.CompletedTask;
    }

    private static string BuildSlug(string name)
    {
        var slug = name.Trim().ToLowerInvariant();
        var sb = new System.Text.StringBuilder();
        foreach (var c in slug)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
            }
            else if (c == ' ' || c == '-')
            {
                sb.Append('-');
            }
        }
        var result = sb.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(result) ? "category" : result;
    }

    private static AdminCategoryDto MapCategory(Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Slug = c.Slug,
        Icon = c.Icon,
        SortOrder = c.SortOrder,
        IsActive = c.IsActive
    };
}
