namespace TaskPilot.Models.Audit;

public record ActivityLogResponse(
    Guid Id,
    Guid TaskId,
    string TaskTitle,
    DateTime Timestamp,
    string FieldChanged,
    string? OldValue,
    string? NewValue,
    string ChangedBy
);

public record ActivityLogQueryParams(
    Guid? TaskId = null,
    DateTime? From = null,
    DateTime? To = null,
    string? FieldChanged = null,
    string? ChangedBy = null,
    int Page = 1,
    int PageSize = 50
);
