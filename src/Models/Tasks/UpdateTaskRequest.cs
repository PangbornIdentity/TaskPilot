using TaskPilot.Models.Enums;

namespace TaskPilot.Models.Tasks;

public record UpdateTaskRequest(
    string Title,
    string? Description,
    string Type,
    TaskPriority Priority,
    Enums.TaskStatus Status,
    TargetDateType TargetDateType,
    DateTime? TargetDate,
    bool IsRecurring,
    RecurrencePattern? RecurrencePattern,
    List<Guid>? TagIds
);
