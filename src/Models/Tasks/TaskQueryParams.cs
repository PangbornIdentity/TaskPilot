using TaskPilot.Models.Enums;

namespace TaskPilot.Models.Tasks;

public record TaskQueryParams(
    Enums.TaskStatus? Status = null,
    int? TaskTypeId = null,
    Area? Area = null,
    TaskPriority? Priority = null,
    string? Search = null,
    List<Guid>? TagIds = null,
    bool? IsRecurring = null,
    int Page = 1,
    int PageSize = 20,
    string SortBy = "priority",
    string SortDir = "asc"
);
