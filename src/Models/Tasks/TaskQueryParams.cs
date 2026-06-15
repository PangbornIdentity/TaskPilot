using TaskPilot.Models.Enums;

namespace TaskPilot.Models.Tasks;

/// <summary>
/// Controls which status bucket the repository targets.
/// Active   → NotStarted, InProgress, Blocked only (same as IncludeOnlyIncomplete).
/// Completed → Completed + Cancelled only.
/// All       → no status restriction (returns every status).
/// </summary>
public enum TaskScope
{
    All = 0,
    Active = 1,
    Completed = 2
}

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
    string SortDir = "asc",
    bool IncludeOnlyIncomplete = false,
    bool OverdueOnly = false,
    TaskScope Scope = TaskScope.All
);
