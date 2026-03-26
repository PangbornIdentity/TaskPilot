using System.Net;
using System.Text.Json;
using TaskPilot.Server.Constants;
using TaskPilot.Shared.DTOs.Common;

namespace TaskPilot.Server.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteErrorResponseAsync(context, ex);
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var error = new ErrorResponse(new ApiError(ErrorCodes.InternalError, "An unexpected error occurred."));
        var json = JsonSerializer.Serialize(error, JsonOptions);
        await context.Response.WriteAsync(json);
    }
}
