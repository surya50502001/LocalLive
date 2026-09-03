using FluentValidation;
using LocalLive.Application.Common.Interfaces;
using LocalLive.Application.Features.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LocalLive.Api.Controllers;

[Route("api/requests")]
[Authorize]
public class RequestsController : ApiControllerBase
{
    private readonly IRequestService _service;
    private readonly IValidator<CreateRequestRequest> _createValidator;

    public RequestsController(IRequestService service, IValidator<CreateRequestRequest> createValidator)
    {
        _service = service;
        _createValidator = createValidator;
    }

    [HttpPost]
    [Authorize(Roles = "Customer,Admin")]
    [EnableRateLimiting("request-create")]
    public async Task<IActionResult> Create([FromBody] CreateRequestRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation);
        }
        return HandleResult(await _service.CreateAsync(RequireUserId(), request));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var role = CurrentRole?.ToString() ?? "Customer";
        return HandleResult(await _service.GetByIdAsync(id, RequireUserId(), role));
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<IActionResult> Cancel(Guid id)
        => HandleResult(await _service.CancelAsync(RequireUserId(), id));

    [HttpPost("{id:guid}/fulfill")]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<IActionResult> Fulfill(Guid id)
        => HandleResult(await _service.FulfillAsync(RequireUserId(), id));

    [HttpPost("{id:guid}/available")]
    [Authorize(Roles = "ShopOwner,Admin")]
    public async Task<IActionResult> Available(Guid id, [FromBody] AvailableRequest? request)
        => HandleResult(await _service.AvailableAsync(RequireUserId(), id, request?.Message));

    [HttpGet("my/live")]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<IActionResult> MyLive()
        => Ok(await _service.GetMyLiveRequestsAsync(RequireUserId()));

    [HttpGet("shop/live")]
    [Authorize(Roles = "ShopOwner,Admin")]
    public async Task<IActionResult> ShopLive()
        => Ok(await _service.GetShopLiveRequestsAsync(RequireUserId()));
}
