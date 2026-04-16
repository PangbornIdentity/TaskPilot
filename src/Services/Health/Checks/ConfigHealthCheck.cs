using System.Diagnostics;
using TaskPilot.Models.Health;

namespace TaskPilot.Services.Health.Checks;

/// <summary>Verifies that required configuration keys are present and non-empty.</summary>
public sealed class ConfigHealthCheck(IConfiguration configuration) : IHealthCheckComponent
{
    private static readonly string[] RequiredKeys =
    [
        "ConnectionStrings:DefaultConnection",
        "Hmac:SecretKey"    // API key HMAC signing secret (stored under Hmac:SecretKey in appsettings)
    ];

    public string Name => "config";
    public bool IsRequired => true;

    public Task<HealthCheckResult> CheckAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var missing = RequiredKeys
            .Where(key => string.IsNullOrWhiteSpace(configuration[key]))
            .ToList();
        sw.Stop();

        if (missing.Count == 0)
        {
            return Task.FromResult(new HealthCheckResult
            {
                Name = Name, Status = HealthStatuses.Healthy, Duration = sw.Elapsed,
                IsRequired = IsRequired, Message = "All required configuration keys present."
            });
        }

        return Task.FromResult(new HealthCheckResult
        {
            Name = Name, Status = HealthStatuses.Unhealthy, Duration = sw.Elapsed,
            IsRequired = IsRequired,
            Message = $"Missing required configuration key(s): {string.Join(", ", missing)}"
        });
    }
}
