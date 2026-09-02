using LocalLive.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalLive.Api.Controllers;

[Route("health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Liveness() => Ok(new { status = "healthy", time = DateTime.UtcNow });

    [HttpGet("ready")]
    public async Task<IActionResult> Readiness([FromServices] AppDbContext db)
    {
        try
        {
            await db.Database.CanConnectAsync();
            return Ok(new { status = "ready", database = "reachable", time = DateTime.UtcNow });
        }
        catch
        {
            return StatusCode(503, new { status = "unready", database = "unreachable" });
        }
    }
}
