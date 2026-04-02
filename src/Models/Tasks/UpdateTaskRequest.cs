using TaskPilot.Models.Enums;

namespace TaskPilot.Models.Tasks;

public record UpdateTaskRequest(
    string Title,
    string? Description,
    int TaskTypeId,
    Area Area,
    TaskPriority Priority,
    Enums.TaskStatus Status,
    TargetDateType TargetDateType,
    DateTime? TargetDate,
    bool IsRecurring,
    RecurrencePattern? RecurrencePattern,
    List<Guid>? TagIds
);
