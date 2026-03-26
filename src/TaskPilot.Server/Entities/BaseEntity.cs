namespace TaskPilot.Server.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }
    public string LastModifiedBy { get; set; } = string.Empty;
}
