using TaskPilot.Shared.Enums;

namespace TaskPilot.Server.Entities;

public class TaskItem : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; }
    public Shared.Enums.TaskStatus Status { get; set; }
    public TargetDateType TargetDateType { get; set; }
    public DateTime? TargetDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? ResultAnalysis { get; set; }
    public bool IsRecurring { get; set; }
    public RecurrencePattern? RecurrencePattern { get; set; }
    public int SortOrder { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string UserId { get; set; } = string.Empty;

    public ICollection<TaskTag> TaskTags { get; set; } = [];
    public ICollection<TaskActivityLog> ActivityLogs { get; set; } = [];
}
