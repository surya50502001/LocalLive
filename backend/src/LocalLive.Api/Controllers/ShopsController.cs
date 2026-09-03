using FluentValidation;
using LocalLive.Application.Common.Interfaces;
using LocalLive.Application.Features.Shops;
using LocalLive.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocalLive.Api.Controllers;

[Route("api/shops")]
[Authorize]
public class ShopsController : ApiControllerBase
{
    private readonly IShopService _service;
    private readonly IValidator<CreateShopRequest> _createValidator;
    private readonly IValidator<UpdateShopRequest> _updateValidator;

    public ShopsController(
        IShopService service,
        IValidator<CreateShopRequest> createValidator,
        IValidator<UpdateShopRequest> updateValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpPost]
    [Authorize(Roles = "ShopOwner,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateShopRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation);
        }
        return HandleResult(await _service.CreateAsync(RequireUserId(), request));
    }

    [HttpGet("me")]
    [Authorize(Roles = "ShopOwner,Admin")]
    public async Task<IActionResult> MyShop()
        => HandleResult(await _service.GetMyShopAsync(RequireUserId()));

    [HttpGet("nearby")]
    [AllowAnonymous]
    public async Task<IActionResult> Nearby([FromQuery] double latitude, [FromQuery] double longitude, [FromQuery] double radiusKm = 10, [FromQuery] Guid? categoryId = null)
        => Ok(await _service.GetNearbyAsync(new NearbyShopQuery
        {
            Latitude = latitude,
            Longitude = longitude,
            RadiusKm = radiusKm,
            CategoryId = categoryId
        }));

    [HttpGet("favorites")]
    public async Task<IActionResult> GetFavorites()
        => Ok(await _service.GetFavoriteShopsAsync(RequireUserId()));

    [HttpPost("{id:guid}/favorite")]
    public async Task<IActionResult> ToggleFavorite(Guid id)
        => HandleResult(await _service.ToggleFavoriteAsync(RequireUserId(), id));

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
        => HandleResult(await _service.GetByIdAsync(id));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ShopOwner,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateShopRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation);
        }
        return HandleResult(await _service.UpdateAsync(RequireUserId(), id, request));
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "ShopOwner,Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateShopStatusRequest request)
        => HandleResult(await _service.UpdateStatusAsync(RequireUserId(), id, request.IsOpen));

    [HttpPut("{id:guid}/online")]
    [Authorize(Roles = "ShopOwner,Admin")]
    public async Task<IActionResult> UpdateOnline(Guid id, [FromBody] UpdateShopOnlineRequest request)
        => HandleResult(await _service.UpdateOnlineStatusAsync(RequireUserId(), id, request.IsOnline));
}
