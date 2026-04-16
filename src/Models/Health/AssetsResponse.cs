namespace TaskPilot.Models.Health;

/// <summary>Response shape for GET /api/v1/health/assets — static asset fingerprint manifest.</summary>
public record AssetsResponse
{
    public required string Version { get; init; }
    public required Dictionary<string, string> Assets { get; init; }
}
