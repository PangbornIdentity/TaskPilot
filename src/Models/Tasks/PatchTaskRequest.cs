using TaskPilot.Models.Enums;

namespace TaskPilot.Models.Tasks;

public record PatchTaskRequest(
    string? Title = null,
    string? Description = null,
    int? TaskTypeId = null,
    Area? Area = null,
    TaskPriority? Priority = null,
    Enums.TaskStatus? Status = null,
    TargetDateType? TargetDateType = null,
    DateTime? TargetDate = null,
    bool? IsRecurring = null,
    RecurrencePattern? RecurrencePattern = null,
    List<Guid>? TagIds = null
);
