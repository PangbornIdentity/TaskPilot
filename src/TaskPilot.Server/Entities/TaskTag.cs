namespace TaskPilot.Server.Entities;

public class TaskTag
{
    public Guid TaskId { get; set; }
    public TaskItem Task { get; set; } = null!;

    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
