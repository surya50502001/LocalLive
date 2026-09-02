using LocalLive.Application.Common;
using LocalLive.Application.Features.Admin;
using LocalLive.Domain.Enums;

namespace LocalLive.Application.Common.Interfaces;

public interface IAdminService
{
    Task<AdminStatsDto> GetStatsAsync();
    Task<PaginatedResult<AdminShopDto>> GetShopsAsync(int page, int pageSize, string? status, string? search);
    Task<Result> VerifyShopAsync(Guid adminUserId, Guid shopId);
    Task<Result> DisableShopAsync(Guid adminUserId, Guid shopId);
    Task<Result> EnableShopAsync(Guid adminUserId, Guid shopId);
    Task<PaginatedResult<AdminUserDto>> GetUsersAsync(int page, int pageSize, string? search);
    Task<Result> BlockUserAsync(Guid adminUserId, Guid userId, string? reason);
    Task<Result> UnblockUserAsync(Guid adminUserId, Guid userId);
    Task<PaginatedResult<AdminRequestDto>> GetRequestsAsync(int page, int pageSize, string? status);
    Task<PaginatedResult<AdminReportDto>> GetReportsAsync(int page, int pageSize, string? status);
    Task<Result> ResolveReportAsync(Guid adminUserId, Guid reportId);
    Task<Result> DismissReportAsync(Guid adminUserId, Guid reportId);
    Task<List<AdminCategoryDto>> ListCategoriesAsync();
    Task<Result<AdminCategoryDto>> CreateCategoryAsync(Guid adminUserId, UpsertCategoryRequest request);
    Task<Result<AdminCategoryDto>> UpdateCategoryAsync(Guid adminUserId, Guid id, UpsertCategoryRequest request);
    Task<Result> DeleteCategoryAsync(Guid adminUserId, Guid id);
}
