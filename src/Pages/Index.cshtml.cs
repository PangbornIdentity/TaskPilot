using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskPilot.Services.Interfaces;
using TaskPilot.Models.Stats;
using TaskPilot.Models.Tasks;
using TaskPilot.Models.Enums;
using TaskStatus = TaskPilot.Models.Enums.TaskStatus;

namespace TaskPilot.Pages;

public class IndexModel(ITaskService taskService, IStatsService statsService) : PageModel
{
    public TaskStatsResponse? Stats { get; private set; }
    public List<TaskResponse> RecentTasks { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        Stats = await statsService.GetTaskStatsAsync(userId);

        var result = await taskService.GetTasksAsync(
            new TaskQueryParams(PageSize: 10, SortBy: "lastModifiedDate", SortDir: "desc"),
            userId);
        RecentTasks = result.Data?.ToList() ?? [];
    }

    public async Task<IActionResult> OnPostQuickAddAsync(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return RedirectToPage();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var modifiedBy = $"user:{User.Identity?.Name}";

        await taskService.CreateTaskAsync(new CreateTaskRequest(
            Title: title.Trim(),
            Description: null,
            TaskTypeId: null,
            Area: Area.Personal,
            Priority: TaskPriority.Medium,
            Status: TaskStatus.NotStarted,
            TargetDateType: TargetDateType.ThisWeek,
            TargetDate: null,
            IsRecurring: false,
            RecurrencePattern: null,
            TagIds: null
        ), userId, modifiedBy);

        TempData["Toast"] = $"Task \"{title.Trim()}\" created.";
        return RedirectToPage();
    }
}
