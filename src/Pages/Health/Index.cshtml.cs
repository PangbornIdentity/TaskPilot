using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskPilot.Models.Health;
using TaskPilot.Services.Health;

namespace TaskPilot.Pages.Health;

/// <summary>
/// Public health dashboard page — anonymous, renders the full health check result.
/// Safe for Azure "Always On" pings and uptime monitors.
/// </summary>
[AllowAnonymous]
public class IndexModel(IHealthService healthService, IConfiguration configuration) : PageModel
{
    public HealthResponse? HealthResult { get; private set; }
    public string? GitHubRepoUrl { get; private set; }
    public DateTime PageRenderedAt { get; private set; }

    public async Task OnGetAsync()
    {
        GitHubRepoUrl = configuration["Diagnostics:GitHubRepoUrl"];
        PageRenderedAt = DateTime.UtcNow;
        try
        {
            HealthResult = await healthService.RunFullAsync(HttpContext.RequestAborted);
        }
        catch (Exception)
        {
            // Page must render even if checks throw
            HealthResult = null;
        }

        // No-cache headers on the page itself
        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";
    }
}
