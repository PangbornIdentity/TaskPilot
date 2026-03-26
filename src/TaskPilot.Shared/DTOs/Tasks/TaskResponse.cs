using TaskPilot.Shared.DTOs.Tags;
using TaskPilot.Shared.Enums;

namespace TaskPilot.Shared.DTOs.Tasks;

public record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    string Type,
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
