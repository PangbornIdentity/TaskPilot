namespace TaskPilot.Models.Health;

/// <summary>Response shape for GET /api/v1/health/ready and /full.</summary>
public record HealthResponse
{
    public required string Status { get; init; }
    public required TimeSpan TotalDuration { get; init; }
    public required VersionResponse Version { get; init; }
    public required List<HealthCheckResult> Checks { get; init; }
}
