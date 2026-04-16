namespace TaskPilot.Models.Health;

/// <summary>Result for a single named health check component.</summary>
public record HealthCheckResult
{
    public required string Name { get; init; }
    public required string Status { get; init; }
    public required TimeSpan Duration { get; init; }
    public string? Message { get; init; }
    public Dictionary<string, string>? Data { get; init; }
    /// <summary>
    /// When true, an unhealthy status rolls up to the overall "unhealthy" (503).
    /// When false, an unhealthy status rolls up to "degraded" (200).
    /// </summary>
    public required bool IsRequired { get; init; }
}
