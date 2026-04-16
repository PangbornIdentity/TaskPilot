using TaskPilot.Models.Health;

namespace TaskPilot.Services.Health;

/// <summary>
/// Orchestrates all registered <see cref="IHealthCheckComponent"/> implementations
/// and aggregates results into a <see cref="HealthResponse"/>.
/// </summary>
public interface IHealthService
{
    /// <summary>Run only required checks (database, migrations, config). Used by /ready.</summary>
    Task<HealthResponse> RunReadinessAsync(CancellationToken ct = default);

    /// <summary>Run all registered checks. Used by /full.</summary>
    Task<HealthResponse> RunFullAsync(CancellationToken ct = default);
}
