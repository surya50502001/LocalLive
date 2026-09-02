using System.Security.Claims;
using LocalLive.Application.Common;
using LocalLive.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LocalLive.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid? CurrentUserId
    {
        get
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    protected UserRole? CurrentRole
    {
        get
        {
            var role = User.FindFirstValue(ClaimTypes.Role)
                       ?? User.FindFirstValue("role");
            return Enum.TryParse<UserRole>(role, true, out var parsed) ? parsed : null;
        }
    }

    protected Guid RequireUserId()
    {
        if (CurrentUserId.HasValue)
        {
            return CurrentUserId.Value;
        }
        throw new UnauthorizedAccessException("User is not authenticated.");
    }

    protected ActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return ToErrorResult(result.Error!);
    }

    protected ActionResult HandleResult(Result result)
        => result.IsSuccess ? NoContent() : ToErrorResult(result.Error!);

    protected ActionResult ValidationProblem(FluentValidation.Results.ValidationResult validation)
    {
        var errors = validation.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
        return BadRequest(new
        {
            type = "https://tools.ietf.org/html/rfc7807",
            title = "ValidationError",
            status = StatusCodes.Status400BadRequest,
            detail = "One or more validation errors occurred.",
            errors
        });
    }

    private ActionResult ToErrorResult(Error error)
    {
        var (status, code) = Middleware.ExceptionHandlingMiddleware.MapError(error);
        return StatusCode(status, new
        {
            type = "https://tools.ietf.org/html/rfc7807",
            title = code,
            status,
            detail = error.Message,
            field = error.Field,
            errors = error.Field is null ? null : new Dictionary<string, string[]> { [error.Field] = new[] { error.Message } }
        });
    }
}
