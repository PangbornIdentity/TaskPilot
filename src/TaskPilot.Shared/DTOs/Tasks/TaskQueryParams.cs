using TaskPilot.Shared.Enums;

namespace TaskPilot.Shared.DTOs.Tasks;

public record TaskQueryParams(
    Enums.TaskStatus? Status = null,
    string? Type = null,
    TaskPriority? Priority = null,
    string? Search = null,
    string? Tags = null,
    bool? IsRecurring = null,
    int Page = 1,
    int PageSize = 20,
    string SortBy = "priority",
    string SortDir = "asc"
);
