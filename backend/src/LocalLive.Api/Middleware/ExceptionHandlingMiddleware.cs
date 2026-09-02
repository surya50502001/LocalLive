using System.Text.Json;
using LocalLive.Application.Common;

namespace LocalLive.Api.Middleware;

/// <summary>
/// Converts Result failures into proper HTTP status codes (RFC 7807 problem details)
/// and unexpected exceptions into 500.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError,
                "InternalServerError", "An unexpected error occurred.", null);
        }
    }

    public static async Task WriteProblemAsync(HttpContext context, int status, string code, string message, string? traceId)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var body = new
        {
            type = "https://tools.ietf.org/html/rfc7807",
            title = code,
            status,
            detail = message,
            traceId = traceId ?? context.TraceIdentifier
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }

    public static (int status, string code) MapError(Error error)
        => error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, error.Code),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, error.Code),
            ErrorType.Forbidden => (StatusCodes.Status403Forbidden, error.Code),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, error.Code),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, error.Code),
            ErrorType.TooManyRequests => (StatusCodes.Status429TooManyRequests, error.Code),
            _ => (StatusCodes.Status500InternalServerError, "InternalServerError")
        };
}
