using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskPilot.Models.Audit;
using TaskPilot.Services.Interfaces;
using TaskPilot.Models.Tasks;
using TaskPilot.Models.Enums;
using TaskStatus = TaskPilot.Models.Enums.TaskStatus;

namespace TaskPilot.Pages.Tasks;

public class TaskDetailModel(ITaskService taskService, IActivityLogService activityLogService) : PageModel
{
    public TaskResponse? Task { get; private set; }
    public IReadOnlyList<ActivityLogResponse> ActivityLogs { get; private set; } = [];

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string ModifiedBy => $"user:{User.Identity?.Name}";

    public async Task OnGetAsync(Guid id)
    {
        Task = await taskService.GetTaskByIdAsync(id, UserId);
        if (Task is not null)
            ActivityLogs = await activityLogService.GetForTaskAsync(id, UserId);
    }

    public async Task<IActionResult> OnPostUpdateAsync(
        Guid id, string title, string? description, string type,
        TaskPriority priority, TaskStatus status,
        TargetDateType targetDateType, DateTime? targetDate)
    {
        await taskService.UpdateTaskAsync(id, new UpdateTaskRequest(
            Title: title,
            Description: description,
            Type: type,
            Priority: priority,
            Status: status,
            TargetDateType: targetDateType,
            TargetDate: targetDate,
            IsRecurring: false,
            RecurrencePattern: null,
            TagIds: null
        ), UserId, ModifiedBy);

        TempData["Toast"] = "Task updated.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCompleteAsync(Guid id)
    {
        await taskService.CompleteTaskAsync(id, new CompleteTaskRequest(), UserId, ModifiedBy);
        TempData["Toast"] = "Task completed!";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await taskService.DeleteTaskAsync(id, UserId, ModifiedBy);
        TempData["Toast"] = "Task deleted.";
        return RedirectToPage("/Tasks/Index");
    }
}
