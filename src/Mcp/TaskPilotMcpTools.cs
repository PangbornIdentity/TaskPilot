using System.ComponentModel;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;
using TaskPilot.Constants;
using TaskPilot.Models.Enums;
using TaskPilot.Models.Tasks;
using TaskPilot.Services.Interfaces;
using TaskStatus = TaskPilot.Models.Enums.TaskStatus;

namespace TaskPilot.Mcp;

[McpServerToolType]
public class TaskPilotMcpTools(
    ITaskService taskService,
    ITagService tagService,
    ITaskTypeService taskTypeService,
    IStatsService statsService,
    IHttpContextAccessor httpContextAccessor)
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    private string UserId =>
        httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User ID not found in claims.");

    private string ModifiedBy
    {
        get
        {
            var keyName = httpContextAccessor.HttpContext!.User
                .FindFirstValue(AuthConstants.ApiKeyClaimType) ?? "unknown";
            return $"api:{keyName}";
        }
    }

    [McpServerTool(Name = "list_tasks")]
    [Description("List tasks for the authenticated user. All filter parameters are optional.")]
    public async Task<string> ListTasksAsync(
        [Description("Filter by status: NotStarted, InProgress, Blocked, Completed, Cancelled")] string? status = null,
        [Description("Filter by area: Personal, Work")] string? area = null,
        [Description("Filter by priority: Critical, High, Medium, Low")] string? priority = null,
        [Description("Search by title or description keyword")] string? search = null,
        [Description("Page number (default 1)")] int page = 1,
        [Description("Page size (default 20, max 100)")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new TaskQueryParams(
            Status: ParseEnum<TaskStatus>(status),
            Area: ParseEnum<Area>(area),
            Priority: ParseEnum<TaskPriority>(priority),
            Search: search,
            Page: page,
            PageSize: Math.Min(pageSize, 100)
        );

        var result = await taskService.GetTasksAsync(queryParams, UserId, cancellationToken);
        return JsonSerializer.Serialize(result, JsonOpts);
    }

    [McpServerTool(Name = "get_task")]
    [Description("Get a single task by its GUID.")]
    public async Task<string> GetTaskAsync(
        [Description("The task GUID")] string id,
        CancellationToken cancellationToken = default)
    {
        var task = await taskService.GetTaskByIdAsync(ParseGuid(id), UserId, cancellationToken);
        if (task is null) throw new InvalidOperationException($"Task '{id}' not found.");
        return JsonSerializer.Serialize(task, JsonOpts);
    }

    [McpServerTool(Name = "create_task")]
    [Description("Create a new task. Required: title, taskTypeId, area, priority, status, targetDateType.")]
    public async Task<string> CreateTaskAsync(
        [Description("Task title (required)")] string title,
        [Description("Task type ID: 1=Task, 2=Goal, 3=Habit, 4=Meeting, 5=Note, 6=Event")] int taskTypeId,
        [Description("Area: Personal or Work")] string area,
        [Description("Priority: Critical, High, Medium, or Low")] string priority,
        [Description("Status: NotStarted, InProgress, Blocked, Completed, or Cancelled")] string status,
        [Description("Target date type: SpecificDay, ThisWeek, or ThisMonth")] string targetDateType,
        [Description("Optional description (supports markdown)")] string? description = null,
        [Description("Specific target date (ISO 8601), only used when targetDateType is SpecificDay")] DateTime? targetDate = null,
        [Description("Whether this task recurs automatically")] bool isRecurring = false,
        [Description("Recurrence pattern if isRecurring: Daily, Weekly, or Monthly")] string? recurrencePattern = null,
        CancellationToken cancellationToken = default)
    {
        var request = new CreateTaskRequest(
            Title: title,
            Description: description,
            TaskTypeId: taskTypeId,
            Area: RequireEnum<Area>(area),
            Priority: RequireEnum<TaskPriority>(priority),
            Status: RequireEnum<TaskStatus>(status),
            TargetDateType: RequireEnum<TargetDateType>(targetDateType),
            TargetDate: targetDate,
            IsRecurring: isRecurring,
            RecurrencePattern: ParseEnum<RecurrencePattern>(recurrencePattern),
            TagIds: null
        );

        var result = await taskService.CreateTaskAsync(request, UserId, ModifiedBy, cancellationToken);
        return JsonSerializer.Serialize(result, JsonOpts);
    }

    [McpServerTool(Name = "update_task")]
    [Description("Partially update a task. Only supplied fields are changed. All parameters are optional except id.")]
    public async Task<string> UpdateTaskAsync(
        [Description("The task GUID")] string id,
        [Description("New title")] string? title = null,
        [Description("New description")] string? description = null,
        [Description("New task type ID")] int? taskTypeId = null,
        [Description("New area: Personal or Work")] string? area = null,
        [Description("New priority: Critical, High, Medium, or Low")] string? priority = null,
        [Description("New status: NotStarted, InProgress, Blocked, Completed, or Cancelled")] string? status = null,
        [Description("New target date type: SpecificDay, ThisWeek, or ThisMonth")] string? targetDateType = null,
        [Description("New target date (ISO 8601)")] DateTime? targetDate = null,
        [Description("Update recurrence flag")] bool? isRecurring = null,
        [Description("New recurrence pattern: Daily, Weekly, or Monthly")] string? recurrencePattern = null,
        CancellationToken cancellationToken = default)
    {
        var request = new PatchTaskRequest(
            Title: title,
            Description: description,
            TaskTypeId: taskTypeId,
            Area: ParseEnum<Area>(area),
            Priority: ParseEnum<TaskPriority>(priority),
            Status: ParseEnum<TaskStatus>(status),
            TargetDateType: ParseEnum<TargetDateType>(targetDateType),
            TargetDate: targetDate,
            IsRecurring: isRecurring,
            RecurrencePattern: ParseEnum<RecurrencePattern>(recurrencePattern),
            TagIds: null
        );

        var result = await taskService.PatchTaskAsync(ParseGuid(id), request, UserId, ModifiedBy, cancellationToken);
        if (result is null) throw new InvalidOperationException($"Task '{id}' not found.");
        return JsonSerializer.Serialize(result, JsonOpts);
    }

    [McpServerTool(Name = "complete_task")]
    [Description("Mark a task as completed with an optional result analysis / reflection.")]
    public async Task<string> CompleteTaskAsync(
        [Description("The task GUID")] string id,
        [Description("Optional reflection: what went well, what didn't, what you'd do differently")] string? resultAnalysis = null,
        CancellationToken cancellationToken = default)
    {
        var result = await taskService.CompleteTaskAsync(
            ParseGuid(id),
            new Models.Tasks.CompleteTaskRequest(resultAnalysis),
            UserId, ModifiedBy, cancellationToken);

        if (result is null) throw new InvalidOperationException($"Task '{id}' not found.");
        return JsonSerializer.Serialize(result, JsonOpts);
    }

    [McpServerTool(Name = "delete_task")]
    [Description("Soft-delete a task. Recoverable within 30 seconds via the web UI undo toast.")]
    public async Task<string> DeleteTaskAsync(
        [Description("The task GUID")] string id,
        CancellationToken cancellationToken = default)
    {
        var deleted = await taskService.DeleteTaskAsync(ParseGuid(id), UserId, ModifiedBy, cancellationToken);
        if (!deleted) throw new InvalidOperationException($"Task '{id}' not found.");
        return JsonSerializer.Serialize(new { success = true, id }, JsonOpts);
    }

    [McpServerTool(Name = "get_stats")]
    [Description("Get aggregated task statistics: active count, completed today, overdue, in progress, blocked, and completion trends.")]
    public async Task<string> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var stats = await statsService.GetTaskStatsAsync(UserId, cancellationToken);
        return JsonSerializer.Serialize(stats, JsonOpts);
    }

    [McpServerTool(Name = "list_tags")]
    [Description("List all tags belonging to the authenticated user.")]
    public async Task<string> ListTagsAsync(CancellationToken cancellationToken = default)
    {
        var tags = await tagService.GetAllTagsAsync(UserId, cancellationToken);
        return JsonSerializer.Serialize(tags, JsonOpts);
    }

    [McpServerTool(Name = "list_task_types")]
    [Description("List all active task types: Task, Goal, Habit, Meeting, Note, Event.")]
    public async Task<string> ListTaskTypesAsync(CancellationToken cancellationToken = default)
    {
        var types = await taskTypeService.GetAllActiveAsync(cancellationToken);
        return JsonSerializer.Serialize(types, JsonOpts);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Guid ParseGuid(string value)
    {
        if (!Guid.TryParse(value, out var guid))
            throw new ArgumentException($"'{value}' is not a valid GUID.");
        return guid;
    }

    private static T? ParseEnum<T>(string? value) where T : struct, Enum
    {
        if (value is null) return null;
        if (!Enum.TryParse<T>(value, ignoreCase: true, out var result))
            throw new ArgumentException($"'{value}' is not a valid {typeof(T).Name}. Valid values: {string.Join(", ", Enum.GetNames<T>())}");
        return result;
    }

    private static T RequireEnum<T>(string value) where T : struct, Enum
    {
        if (!Enum.TryParse<T>(value, ignoreCase: true, out var result))
            throw new ArgumentException($"'{value}' is not a valid {typeof(T).Name}. Valid values: {string.Join(", ", Enum.GetNames<T>())}");
        return result;
    }
}
