using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskPilot.Models.Audit;
using TaskPilot.Services.Interfaces;
using TaskPilot.Models.Tasks;
using TaskPilot.Models.TaskTypes;
using TaskPilot.Models.Tags;
using TaskPilot.Models.Enums;
using TaskStatus = TaskPilot.Models.Enums.TaskStatus;

namespace TaskPilot.Pages.Tasks;

public class TaskDetailModel(
    ITaskService taskService,
    IActivityLogService activityLogService,
    ITaskTypeService taskTypeService,
    ITagService tagService) : PageModel
{
    public TaskResponse? Task { get; private set; }
    public IReadOnlyList<ActivityLogResponse> ActivityLogs { get; private set; } = [];
    public IReadOnlyList<TaskTypeResponse> TaskTypes { get; private set; } = [];
    public IReadOnlyList<TagResponse> AllTags { get; private set; } = [];

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string ModifiedBy => $"user:{User.Identity?.Name}";

    public async Task OnGetAsync(Guid id)
    {
        Task = await taskService.GetTaskByIdAsync(id, UserId);
        TaskTypes = await taskTypeService.GetAllActiveAsync();
        AllTags = (await tagService.GetAllTagsAsync(UserId)).ToList();
        if (Task is not null)
            ActivityLogs = await activityLogService.GetForTaskAsync(id, UserId);
    }

    public async Task<IActionResult> OnPostUpdateAsync(
        Guid id, string title, string? description, int taskTypeId, Area area,
        TaskPriority priority, TaskStatus status,
        TargetDateType targetDateType, DateTime? targetDate,
        List<Guid>? tagIds)
    {
        // The edit form has no recurrence fields, so carry the stored values through
        // instead of resetting recurrence set via the API or MCP.
        var existing = await taskService.GetTaskByIdAsync(id, UserId);
        if (existing is null) return RedirectToPage(new { id });

        await taskService.UpdateTaskAsync(id, new UpdateTaskRequest(
            Title: title,
            Description: description,
            TaskTypeId: taskTypeId,
            Area: area,
            Priority: priority,
            Status: status,
            TargetDateType: targetDateType,
            TargetDate: targetDate,
            IsRecurring: existing.IsRecurring,
            RecurrencePattern: existing.RecurrencePattern,
            TagIds: tagIds
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
