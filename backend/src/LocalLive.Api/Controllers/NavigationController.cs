using LocalLive.Application.Common.Interfaces;
using LocalLive.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocalLive.Api.Controllers;

[Route("api/navigation")]
public class NavigationController : ApiControllerBase
{
    private readonly INavigationService _navigationService;

    public NavigationController(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    [HttpGet("route")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRoute(
        [FromQuery] double fromLat,
        [FromQuery] double fromLng,
        [FromQuery] double toLat,
        [FromQuery] double toLng,
        [FromQuery] string mode = "walking")
    {
        if (fromLat < -90 || fromLat > 90 || fromLng < -180 || fromLng > 180 ||
            toLat < -90 || toLat > 90 || toLng < -180 || toLng > 180)
        {
            return BadRequest(new { detail = "Invalid coordinates provided." });
        }

        var from = new GeoPoint(fromLat, fromLng);
        var to = new GeoPoint(toLat, toLng);

        var result = await _navigationService.CalculateRouteAsync(from, to, mode, HttpContext.RequestAborted);
        return Ok(result);
    }
}
