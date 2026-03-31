namespace TaskPilot.Entities;

public class ApiKey : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public DateTime? LastUsedDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string UserId { get; set; } = string.Empty;

    public ICollection<ApiAuditLog> AuditLogs { get; set; } = [];
}
