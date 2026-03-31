using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskPilot.Services.Interfaces;
using TaskPilot.Models.Tasks;
using TaskPilot.Models.Enums;
using TaskStatus = TaskPilot.Models.Enums.TaskStatus;

namespace TaskPilot.Pages.Tasks;

public class TasksIndexModel(ITaskService taskService) : PageModel
{
    public List<TaskResponse> Tasks { get; private set; } = [];
    public Dictionary<string, List<TaskResponse>> KanbanColumns { get; private set; } = [];
    public int TotalCount { get; private set; }
    public string View { get; private set; } = "list";
    public string? Search { get; private set; }
    public TaskStatus? StatusFilter { get; private set; }
    public TaskPriority? PriorityFilter { get; private set; }

    public async Task OnGetAsync(
        string? view = "list",
        string? search = null,
        TaskStatus? status = null,
        TaskPriority? priority = null,
        int page = 1)
    {
        View = view ?? "list";
        Search = search;
        StatusFilter = status;
        PriorityFilter = priority;

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        var result = await taskService.GetTasksAsync(new TaskQueryParams(
            Status: status,
            Priority: priority,
            Search: search,
            Page: page,
            PageSize: 50,
            SortBy: "priority",
            SortDir: "asc"
        ), userId);

        Tasks = result.Data?.ToList() ?? [];
        TotalCount = result.Meta?.TotalCount ?? Tasks.Count;

        if (View == "board")
        {
            KanbanColumns = new Dictionary<string, List<TaskResponse>>
            {
                ["Not Started"] = Tasks.Where(t => t.Status == TaskStatus.NotStarted).ToList(),
                ["In Progress"]  = Tasks.Where(t => t.Status == TaskStatus.InProgress).ToList(),
                ["Blocked"]      = Tasks.Where(t => t.Status == TaskStatus.Blocked).ToList(),
                ["Completed"]    = Tasks.Where(t => t.Status == TaskStatus.Completed).ToList(),
            };
        }
    }

    public async Task<IActionResult> OnPostCreateAsync(
        string title, string? description, string type,
        TaskPriority priority, TaskStatus status,
        TargetDateType targetDateType, DateTime? targetDate)
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
            Type: string.IsNullOrWhiteSpace(type) ? "Task" : type,
            Priority: priority,
            Status: status,
            TargetDateType: targetDateType,
            TargetDate: targetDate,
            IsRecurring: false,
            RecurrencePattern: null,
            TagIds: null
        ), userId, modifiedBy);

        TempData["Toast"] = $"Task \"{title.Trim()}\" created.";
        return RedirectToPage();
    }
}
