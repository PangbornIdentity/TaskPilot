using TaskPilot.Server.Middleware;

namespace TaskPilot.Server.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionMiddleware>();

    public static IApplicationBuilder UseApiAudit(this IApplicationBuilder app)
        => app.UseMiddleware<ApiAuditMiddleware>();
}
