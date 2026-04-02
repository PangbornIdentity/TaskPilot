using TaskPilot.Models.Tags;
using TaskPilot.Models.Enums;

namespace TaskPilot.Models.Tasks;

public record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    int TaskTypeId,
    string? TaskTypeName,
    Area Area,
    string AreaName,
    TaskPriority Priority,
    Enums.TaskStatus Status,
    TargetDateType TargetDateType,
    DateTime? TargetDate,
    DateTime? CompletedDate,
    string? ResultAnalysis,
    bool IsRecurring,
    RecurrencePattern? RecurrencePattern,
    int SortOrder,
    DateTime CreatedDate,
    DateTime LastModifiedDate,
    string LastModifiedBy,
    string UserId,
    IReadOnlyList<TagResponse> Tags
);
