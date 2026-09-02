using LocalLive.Application.Common.Interfaces;
using LocalLive.Application.Features.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocalLive.Api.Controllers;

[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ApiControllerBase
{
    private readonly IAdminService _service;

    public AdminController(IAdminService service)
    {
        _service = service;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
        => Ok(await _service.GetStatsAsync());

    [HttpGet("shops")]
    public async Task<IActionResult> Shops([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null, [FromQuery] string? search = null)
        => Ok(await _service.GetShopsAsync(page, pageSize, status, search));

    [HttpPost("shops/{id:guid}/verify")]
    public async Task<IActionResult> VerifyShop(Guid id)
        => HandleResult(await _service.VerifyShopAsync(RequireUserId(), id));

    [HttpPost("shops/{id:guid}/disable")]
    public async Task<IActionResult> DisableShop(Guid id)
        => HandleResult(await _service.DisableShopAsync(RequireUserId(), id));

    [HttpPost("shops/{id:guid}/enable")]
    public async Task<IActionResult> EnableShop(Guid id)
        => HandleResult(await _service.EnableShopAsync(RequireUserId(), id));

    [HttpGet("users")]
    public async Task<IActionResult> Users([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
        => Ok(await _service.GetUsersAsync(page, pageSize, search));

    [HttpPost("users/{id:guid}/block")]
    public async Task<IActionResult> BlockUser(Guid id, [FromBody] BlockUserRequest? request)
        => HandleResult(await _service.BlockUserAsync(RequireUserId(), id, request?.Reason));

    [HttpPost("users/{id:guid}/unblock")]
    public async Task<IActionResult> UnblockUser(Guid id)
        => HandleResult(await _service.UnblockUserAsync(RequireUserId(), id));

    [HttpGet("requests")]
    public async Task<IActionResult> Requests([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null)
        => Ok(await _service.GetRequestsAsync(page, pageSize, status));

    [HttpGet("reports")]
    public async Task<IActionResult> Reports([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null)
        => Ok(await _service.GetReportsAsync(page, pageSize, status));

    [HttpPost("reports/{id:guid}/resolve")]
    public async Task<IActionResult> ResolveReport(Guid id)
        => HandleResult(await _service.ResolveReportAsync(RequireUserId(), id));

    [HttpPost("reports/{id:guid}/dismiss")]
    public async Task<IActionResult> DismissReport(Guid id)
        => HandleResult(await _service.DismissReportAsync(RequireUserId(), id));

    [HttpGet("categories")]
    public async Task<IActionResult> ListCategories()
        => Ok(await _service.ListCategoriesAsync());

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] UpsertCategoryRequest request)
        => HandleResult(await _service.CreateCategoryAsync(RequireUserId(), request));

    [HttpPut("categories/{id:guid}")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpsertCategoryRequest request)
        => HandleResult(await _service.UpdateCategoryAsync(RequireUserId(), id, request));

    [HttpDelete("categories/{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
        => HandleResult(await _service.DeleteCategoryAsync(RequireUserId(), id));
}
