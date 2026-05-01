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
    // View is now display mode only — "list" or "board". Never "incomplete".
    public string View { get; private set; } = "list";
    public string? Search { get; private set; }
    public TaskStatus? StatusFilter { get; private set; }
    public TaskPriority? PriorityFilter { get; private set; }
    public Area? AreaFilter { get; private set; }
    public int? TaskTypeIdFilter { get; private set; }
    public List<Guid>? TagIdFilter { get; private set; }
    public bool IncompleteFilter { get; private set; }
    public bool OverdueFilter { get; private set; }
    // SortBy/SortDir are nullable to distinguish "URL explicitly set this column" from
    // "no sort param in URL (fall back to repo default)". HeaderSortState uses null to
    // know that no header is active, so the first click cycles to asc rather than desc.
    public string? SortBy { get; private set; }
    public string? SortDir { get; private set; }
    public IReadOnlyList<TaskTypeResponse> TaskTypes { get; private set; } = [];
    public IReadOnlyList<TagResponse> AllTags { get; private set; } = [];

    public async Task OnGetAsync(
        string? view = "list",
        string? search = null,
        TaskStatus? status = null,
        TaskPriority? priority = null,
        Area? area = null,
        int? taskTypeId = null,
        List<Guid>? tagIds = null,
        bool incomplete = false,
        bool overdue = false,
        string? sortBy = null,
        string? sortDir = null,
        int page = 1)
    {
        // View is display mode only ("list" or "board"). Anything unrecognized falls back to "list".
        View = view == "board" ? "board" : "list";
        Search = search;
        StatusFilter = status;
        PriorityFilter = priority;
        AreaFilter = area;
        TaskTypeIdFilter = taskTypeId;
        TagIdFilter = tagIds;
        IncompleteFilter = incomplete;
        OverdueFilter = overdue;
        SortBy = string.IsNullOrWhiteSpace(sortBy) ? null : sortBy.ToLowerInvariant();
        SortDir = string.IsNullOrWhiteSpace(sortDir)
            ? null
            : (string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        TaskTypes = await taskTypeService.GetAllActiveAsync();
        AllTags = (await tagService.GetAllTagsAsync(userId)).ToList();

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
            IncludeOnlyIncomplete: incomplete,
            OverdueOnly: overdue
        ), userId);

        Tasks = result.Data?.ToList() ?? [];
        TotalCount = result.Meta?.TotalCount ?? Tasks.Count;

        if (View == "board")
        {
            // When the Incomplete chip is on, the kanban renders only the three "open"
            // columns. Completed and Cancelled are hidden entirely (not just emptied) so
            // the user gets the canonical "what to work on" board. Wireframes Page 4
            // covers this composition.
            KanbanColumns = incomplete
                ? new Dictionary<string, List<TaskResponse>>
                {
                    ["Not Started"] = Tasks.Where(t => t.Status == TaskStatus.NotStarted).ToList(),
                    ["In Progress"]  = Tasks.Where(t => t.Status == TaskStatus.InProgress).ToList(),
                    ["Blocked"]      = Tasks.Where(t => t.Status == TaskStatus.Blocked).ToList(),
                }
                : new Dictionary<string, List<TaskResponse>>
                {
                    ["Not Started"] = Tasks.Where(t => t.Status == TaskStatus.NotStarted).ToList(),
                    ["In Progress"]  = Tasks.Where(t => t.Status == TaskStatus.InProgress).ToList(),
                    ["Blocked"]      = Tasks.Where(t => t.Status == TaskStatus.Blocked).ToList(),
                    ["Completed"]    = Tasks.Where(t => t.Status == TaskStatus.Completed).ToList(),
                };
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
}
