using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskPilot.Services.Interfaces;
using TaskPilot.Models.Tasks;
using TaskPilot.Models.TaskTypes;
using TaskPilot.Models.Tags;
using TaskPilot.Models.Enums;
using TaskStatus = TaskPilot.Models.Enums.TaskStatus;

namespace TaskPilot.Pages.Tasks;

public class TasksIndexModel(ITaskService taskService, ITaskTypeService taskTypeService, ITagService tagService) : PageModel
{
    public List<TaskResponse> Tasks { get; private set; } = [];
    public Dictionary<string, List<TaskResponse>> KanbanColumns { get; private set; } = [];
    public int TotalCount { get; private set; }
    // View is display mode only — "list" or "board".
    public string View { get; private set; } = "list";
    // Show controls which status bucket is visible: "active" (default), "completed", or "all".
    public string Show { get; private set; } = "active";
    public string? Search { get; private set; }
    public TaskStatus? StatusFilter { get; private set; }
    public TaskPriority? PriorityFilter { get; private set; }
    public Area? AreaFilter { get; private set; }
    public int? TaskTypeIdFilter { get; private set; }
    public List<Guid>? TagIdFilter { get; private set; }
    public bool OverdueFilter { get; private set; }
    // SortBy/SortDir are nullable to distinguish "URL explicitly set this column" from
    // "no sort param in URL (fall back to repo default)". HeaderSortState uses null to
    // know that no header is active, so the first click cycles to asc rather than desc.
    public string? SortBy { get; private set; }
    public string? SortDir { get; private set; }
    public IReadOnlyList<TaskTypeResponse> TaskTypes { get; private set; } = [];
    public IReadOnlyList<TagResponse> AllTags { get; private set; } = [];

    // Determines whether any filter is active (beyond the default show=active).
    // Used by the Reset filters link visibility logic in the view.
    public bool HasActiveFilters =>
        Show != "active"
        || StatusFilter.HasValue
        || PriorityFilter.HasValue
        || AreaFilter.HasValue
        || TaskTypeIdFilter.HasValue
        || (TagIdFilter?.Count > 0)
        || OverdueFilter
        || !string.IsNullOrEmpty(SortBy);

    public async Task OnGetAsync(
        string? view = "list",
        string? show = "active",
        string? search = null,
        TaskStatus? status = null,
        TaskPriority? priority = null,
        Area? area = null,
        int? taskTypeId = null,
        List<Guid>? tagIds = null,
        bool overdue = false,
        string? sortBy = null,
        string? sortDir = null,
        int page = 1)
    {
        // View is display mode only ("list" or "board"). Anything unrecognized falls back to "list".
        View = view == "board" ? "board" : "list";

        // Show controls the status scope. Default is "active" (NotStarted, InProgress, Blocked).
        Show = show switch
        {
            "completed" => "completed",
            "all"       => "all",
            _           => "active"   // default — omitted or unrecognized
        };

        Search = search;
        StatusFilter = status;
        PriorityFilter = priority;
        AreaFilter = area;
        TaskTypeIdFilter = taskTypeId;
        TagIdFilter = tagIds;
        OverdueFilter = overdue;
        SortBy = string.IsNullOrWhiteSpace(sortBy) ? null : sortBy.ToLowerInvariant();
        SortDir = string.IsNullOrWhiteSpace(sortDir)
            ? null
            : (string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        TaskTypes = await taskTypeService.GetAllActiveAsync();
        AllTags = (await tagService.GetAllTagsAsync(userId)).ToList();

        // Translate the "show" scope to the repository's IncludeOnlyIncomplete flag
        // or a status-set filter applied at the page-model layer. The repository already
        // supports IncludeOnlyIncomplete (the active set). For "completed" we apply a
        // post-query status filter at this layer to avoid a new repo parameter.
        var includeOnlyIncomplete = Show == "active";

        var result = await taskService.GetTasksAsync(new TaskQueryParams(
            Status: status,
            TaskTypeId: taskTypeId,
            Area: area,
            Priority: priority,
            Search: search,
            TagIds: tagIds,
            Page: page,
            PageSize: 50,
            // The repository default sort kicks in when SortBy is "priority" — pass that
            // when no header is active so existing behaviour stays.
            SortBy: SortBy ?? "priority",
            SortDir: SortDir ?? "asc",
            IncludeOnlyIncomplete: includeOnlyIncomplete,
            OverdueOnly: overdue
        ), userId);

        var allFetched = result.Data?.ToList() ?? [];

        // For "completed" show-mode, narrow to the terminal statuses at the page layer.
        // IncludeOnlyIncomplete=false already returns everything; we just post-filter.
        Tasks = Show == "completed"
            ? allFetched.Where(t => t.Status == TaskStatus.Completed
                                 || t.Status == TaskStatus.Cancelled).ToList()
            : allFetched;

        TotalCount = Tasks.Count;

        if (View == "board")
        {
            KanbanColumns = BuildKanbanColumns();
        }
    }

    public async Task<IActionResult> OnPostCreateAsync(
        string title, string? description, int taskTypeId, Area area,
        TaskPriority priority, TaskStatus status,
        TargetDateType targetDateType, DateTime? targetDate,
        List<Guid>? tagIds)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["Error"] = "Title is required.";
            return RedirectToPage();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var modifiedBy = $"user:{User.Identity?.Name}";

        await taskService.CreateTaskAsync(new CreateTaskRequest(
            Title: title.Trim(),
            Description: description,
            TaskTypeId: taskTypeId,
            Area: area,
            Priority: priority,
            Status: status,
            TargetDateType: targetDateType,
            TargetDate: targetDate,
            IsRecurring: false,
            RecurrencePattern: null,
            TagIds: tagIds
        ), userId, modifiedBy);

        TempData["Toast"] = $"Task \"{title.Trim()}\" created.";
        return RedirectToPage();
    }

    // Build the kanban column map honoring the current Show value.
    // active → three open columns only.
    // completed → Completed + Cancelled columns only.
    // all → all five columns.
    private Dictionary<string, List<TaskResponse>> BuildKanbanColumns() => Show switch
    {
        "completed" => new Dictionary<string, List<TaskResponse>>
        {
            ["Completed"]  = Tasks.Where(t => t.Status == TaskStatus.Completed).ToList(),
            ["Cancelled"]  = Tasks.Where(t => t.Status == TaskStatus.Cancelled).ToList(),
        },
        "all" => new Dictionary<string, List<TaskResponse>>
        {
            ["Not Started"] = Tasks.Where(t => t.Status == TaskStatus.NotStarted).ToList(),
            ["In Progress"]  = Tasks.Where(t => t.Status == TaskStatus.InProgress).ToList(),
            ["Blocked"]      = Tasks.Where(t => t.Status == TaskStatus.Blocked).ToList(),
            ["Completed"]    = Tasks.Where(t => t.Status == TaskStatus.Completed).ToList(),
            ["Cancelled"]    = Tasks.Where(t => t.Status == TaskStatus.Cancelled).ToList(),
        },
        _ => new Dictionary<string, List<TaskResponse>>   // "active" (default)
        {
            ["Not Started"] = Tasks.Where(t => t.Status == TaskStatus.NotStarted).ToList(),
            ["In Progress"]  = Tasks.Where(t => t.Status == TaskStatus.InProgress).ToList(),
            ["Blocked"]      = Tasks.Where(t => t.Status == TaskStatus.Blocked).ToList(),
        }
    };
}
