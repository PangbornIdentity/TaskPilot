using System.Diagnostics;
using TaskPilot.Diagnostics;
using TaskPilot.Models.Health;

namespace TaskPilot.Services.Health;

/// <summary>
/// Orchestrates all registered <see cref="IHealthCheckComponent"/> implementations.
/// Ready check runs only required checks; full check runs all checks.
/// Aggregation: unhealthy if any required check fails → 503; degraded if only optional fail → 200.
/// </summary>
public sealed class HealthService(
    IEnumerable<IHealthCheckComponent> checks,
    IWebHostEnvironment env) : IHealthService
{
    private static VersionResponse BuildVersionResponse(IWebHostEnvironment env) => new()
    {
        Version = BuildInfo.Version,
        GitCommit = BuildInfo.GitCommit,
        GitCommitShort = BuildInfo.GitCommitShort,
        BuildTimestampUtc = BuildInfo.BuildTimestampUtc,
        Environment = env.EnvironmentName,
        MachineName = System.Environment.MachineName,
        Uptime = DateTime.UtcNow - ProcessUptime.StartTime
    };

    /// <inheritdoc/>
    public async Task<HealthResponse> RunReadinessAsync(CancellationToken ct = default)
    {
        var requiredChecks = checks.Where(c => c.IsRequired);
        return await RunChecksAsync(requiredChecks, ct);
    }

    /// <inheritdoc/>
    public async Task<HealthResponse> RunFullAsync(CancellationToken ct = default) =>
        await RunChecksAsync(checks, ct);

    private async Task<HealthResponse> RunChecksAsync(
        IEnumerable<IHealthCheckComponent> componentsToRun, CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

        var sw = Stopwatch.StartNew();
        var results = new List<HealthCheckResult>();

        foreach (var component in componentsToRun)
        {
            var result = await component.CheckAsync(timeoutCts.Token);
            results.Add(result);
        }

        sw.Stop();

        var status = AggregateStatus(results);

        return new HealthResponse
        {
            Status = status,
            TotalDuration = sw.Elapsed,
            Version = BuildVersionResponse(env),
            Checks = results
        };
    }

    private static string AggregateStatus(List<HealthCheckResult> results)
    {
        if (results.Any(r => r.IsRequired && r.Status == HealthStatuses.Unhealthy))
            return HealthStatuses.Unhealthy;

        if (results.Any(r => r.Status == HealthStatuses.Unhealthy))
            return HealthStatuses.Degraded;

        return HealthStatuses.Healthy;
    }
}
