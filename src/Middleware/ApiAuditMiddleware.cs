using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TaskPilot.Constants;
using TaskPilot.Entities;
using TaskPilot.Repositories.Interfaces;

namespace TaskPilot.Middleware;

public class ApiAuditMiddleware(RequestDelegate next, ILogger<ApiAuditMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IAuditLogRepository auditLogRepository)
    {
        // Only audit /api/v1/ requests authenticated via API key
        if (!context.Request.Path.StartsWithSegments("/api/v1") ||
            context.User.FindFirstValue(ClaimTypes.AuthenticationMethod) != AuthConstants.ApiKeyScheme)
        {
            await next(context);
            return;
        }

        var sw = Stopwatch.StartNew();

        // Buffer request body for hashing
        context.Request.EnableBuffering();
        var bodyHash = await HashRequestBodyAsync(context.Request);
        context.Request.Body.Position = 0;

        await next(context);

        sw.Stop();

        var apiKeyName = context.User.FindFirstValue(AuthConstants.ApiKeyClaimType) ?? "unknown";
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var apiKeyId = context.Items["ApiKeyId"] as Guid?;

        if (apiKeyId is null)
        {
            logger.LogWarning("ApiAuditMiddleware: ApiKeyId not found in context items for request {Path}", context.Request.Path);
            return;
        }

        var auditLog = new ApiAuditLog
        {
            ApiKeyId = apiKeyId.Value,
            ApiKeyName = apiKeyName,
            Timestamp = DateTime.UtcNow,
            HttpMethod = context.Request.Method,
            Endpoint = context.Request.Path.Value ?? string.Empty,
            RequestBodyHash = bodyHash,
            ResponseStatusCode = context.Response.StatusCode,
            DurationMs = sw.ElapsedMilliseconds,
            UserId = userId
        };

        try
        {
            await auditLogRepository.AddAsync(auditLog);
        }
        catch (Exception ex)
        {
            // Non-blocking — audit failure must never affect the API response
            logger.LogError(ex, "Failed to write API audit log for {Method} {Path}", context.Request.Method, context.Request.Path);
        }
    }

    private static async Task<string> HashRequestBodyAsync(HttpRequest request)
    {
        if (!request.ContentLength.HasValue || request.ContentLength == 0)
            return string.Empty;

        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(body));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
