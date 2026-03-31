namespace TaskPilot.Entities;

public class Tag : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;

    public ICollection<TaskTag> TaskTags { get; set; } = [];
}
