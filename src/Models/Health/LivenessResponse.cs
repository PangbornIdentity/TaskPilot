namespace TaskPilot.Models.Health;

/// <summary>Response shape for GET /api/v1/health/live.</summary>
public record LivenessResponse
{
    public required string Status { get; init; }
    public required DateTime TimestampUtc { get; init; }
}
