namespace TaskPilot.Server.Entities;

public class ApiAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApiKeyId { get; set; }
    public ApiKey ApiKey { get; set; } = null!;
    public string ApiKeyName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string HttpMethod { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string RequestBodyHash { get; set; } = string.Empty;
    public int ResponseStatusCode { get; set; }
    public long DurationMs { get; set; }
    public string UserId { get; set; } = string.Empty;
}
