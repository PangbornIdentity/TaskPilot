using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using TaskPilot.Constants;
using TaskPilot.Models.Health;

namespace TaskPilot.Services.Health.Checks;

/// <summary>
/// Verifies that both authentication schemes are registered:
/// <c>Identity.Application</c> (cookie) and <c>ApiKey</c>.
/// </summary>
public sealed class AuthHandlersHealthCheck(IAuthenticationSchemeProvider schemeProvider) : IHealthCheckComponent
{
    public string Name => "auth-handlers";
    public bool IsRequired => true;

    public async Task<HealthCheckResult> CheckAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var schemes = await schemeProvider.GetAllSchemesAsync();
        var names = schemes.Select(s => s.Name).ToHashSet();
        sw.Stop();

        var missing = new List<string>();
        if (!names.Contains(AuthConstants.CookieScheme))
            missing.Add(AuthConstants.CookieScheme);
        if (!names.Contains(AuthConstants.ApiKeyScheme))
            missing.Add(AuthConstants.ApiKeyScheme);

        if (missing.Count == 0)
        {
            return new HealthCheckResult
            {
                Name = Name, Status = HealthStatuses.Healthy, Duration = sw.Elapsed,
                IsRequired = IsRequired, Message = "Both authentication schemes registered."
            };
        }

        return new HealthCheckResult
        {
            Name = Name, Status = HealthStatuses.Unhealthy, Duration = sw.Elapsed,
            IsRequired = IsRequired,
            Message = $"Missing authentication scheme(s): {string.Join(", ", missing)}"
        };
    }
}
