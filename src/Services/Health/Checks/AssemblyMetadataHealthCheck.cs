using System.Diagnostics;
using TaskPilot.Diagnostics;
using TaskPilot.Models.Health;

namespace TaskPilot.Services.Health.Checks;

/// <summary>
/// Verifies that the MSBuild git-stamp target ran successfully at build time
/// (i.e., <see cref="BuildInfo.GitCommit"/> is not "unknown").
/// </summary>
public sealed class AssemblyMetadataHealthCheck : IHealthCheckComponent
{
    public string Name => "assembly-metadata";
    public bool IsRequired => false;

    public Task<HealthCheckResult> CheckAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var isStamped = BuildInfo.GitCommit != "unknown";
        sw.Stop();

        return Task.FromResult(new HealthCheckResult
        {
            Name = Name,
            Status = isStamped ? HealthStatuses.Healthy : HealthStatuses.Unhealthy,
            Duration = sw.Elapsed,
            IsRequired = IsRequired,
            Message = isStamped
                ? $"Assembly stamped with git commit {BuildInfo.GitCommitShort}."
                : "Assembly was built without git metadata (GitCommit = \"unknown\"). Check build environment."
        });
    }
}
