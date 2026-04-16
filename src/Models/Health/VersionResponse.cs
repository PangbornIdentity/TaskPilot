namespace TaskPilot.Models.Health;

/// <summary>Response shape for GET /api/v1/health/version.</summary>
public record VersionResponse
{
    public required string Version { get; init; }
    public required string GitCommit { get; init; }
    public required string GitCommitShort { get; init; }
    public required DateTime BuildTimestampUtc { get; init; }
    public required string Environment { get; init; }
    public required string MachineName { get; init; }
    public required TimeSpan Uptime { get; init; }
}
