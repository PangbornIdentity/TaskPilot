using TaskPilot.Models.Health;

namespace TaskPilot.Services.Health;

/// <summary>
/// Contract for a single named diagnostic check.
/// Implementations are registered with DI and invoked by <see cref="IHealthService"/>.
/// </summary>
public interface IHealthCheckComponent
{
    /// <summary>Unique lower-kebab-case identifier, e.g. "database".</summary>
    string Name { get; }

    /// <summary>
    /// When true an unhealthy result rolls the aggregate status to "unhealthy" (503).
    /// When false an unhealthy result rolls only to "degraded" (200).
    /// </summary>
    bool IsRequired { get; }

    /// <summary>Execute the check. Must complete within the supplied <paramref name="ct"/>.</summary>
    Task<HealthCheckResult> CheckAsync(CancellationToken ct = default);
}
