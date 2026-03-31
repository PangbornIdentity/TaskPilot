using TaskPilot.Middleware;

namespace TaskPilot.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionMiddleware>();

    public static IApplicationBuilder UseApiAudit(this IApplicationBuilder app)
        => app.UseMiddleware<ApiAuditMiddleware>();
}
