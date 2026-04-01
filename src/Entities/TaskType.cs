namespace TaskPilot.Entities;

public class TaskType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<TaskItem> Tasks { get; set; } = [];
}
