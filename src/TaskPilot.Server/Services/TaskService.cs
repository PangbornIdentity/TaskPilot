using TaskPilot.Server.Entities;
using TaskPilot.Server.Repositories.Interfaces;
using TaskPilot.Server.Services.Interfaces;
using TaskPilot.Shared.DTOs.Common;
using TaskPilot.Shared.DTOs.Tags;
using TaskPilot.Shared.DTOs.Tasks;
using TaskPilot.Shared.Enums;

namespace TaskPilot.Server.Services;

public class TaskService(ITaskRepository taskRepository, ITagRepository tagRepository) : ITaskService
{
    public async Task<PagedApiResponse<TaskResponse>> GetTasksAsync(TaskQueryParams queryParams, string userId, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await taskRepository.GetPagedAsync(queryParams, userId, cancellationToken);
        var responses = items.Select(MapToResponse).ToList();
        var totalPages = (int)Math.Ceiling(totalCount / (double)queryParams.PageSize);
        var meta = new PagedResponseMeta(DateTime.UtcNow, Guid.NewGuid().ToString(), queryParams.Page, queryParams.PageSize, totalCount, totalPages);
        return new PagedApiResponse<TaskResponse>(responses, meta);
    }

    public async Task<TaskResponse?> GetTaskByIdAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        var task = await taskRepository.GetByIdWithDetailsAsync(id, userId, cancellationToken);
        return task is null ? null : MapToResponse(task);
    }

    public async Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, string userId, string modifiedBy, CancellationToken cancellationToken = default)
    {
        var sortOrder = await taskRepository.GetMaxSortOrderAsync(userId, cancellationToken) + 1;

        var task = new TaskItem
        {
            Title = request.Title,
            Description = request.Description,
            Type = request.Type,
            Priority = request.Priority,
            Status = request.Status,
            TargetDateType = request.TargetDateType,
            TargetDate = request.TargetDate,
            IsRecurring = request.IsRecurring,
            RecurrencePattern = request.RecurrencePattern,
            SortOrder = sortOrder,
            UserId = userId,
            LastModifiedBy = modifiedBy
        };

        if (request.TagIds?.Count > 0)
        {
            var tags = await tagRepository.GetByIdsAsync(request.TagIds, userId, cancellationToken);
            task.TaskTags = tags.Select(t => new TaskTag { TagId = t.Id }).ToList();
        }

        await taskRepository.AddAsync(task, cancellationToken);
        await taskRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(task);
    }

    public async Task<TaskResponse?> UpdateTaskAsync(Guid id, UpdateTaskRequest request, string userId, string modifiedBy, CancellationToken cancellationToken = default)
    {
        var task = await taskRepository.GetByIdWithTagsAsync(id, userId, cancellationToken);
        if (task is null) return null;

        var changes = BuildActivityLogs(task, request, modifiedBy);

        task.Title = request.Title;
        task.Description = request.Description;
        task.Type = request.Type;
        task.Priority = request.Priority;
        task.Status = request.Status;
        task.TargetDateType = request.TargetDateType;
        task.TargetDate = request.TargetDate;
        task.IsRecurring = request.IsRecurring;
        task.RecurrencePattern = request.RecurrencePattern;
        task.LastModifiedBy = modifiedBy;

        task.TaskTags.Clear();
        if (request.TagIds?.Count > 0)
        {
            var tags = await tagRepository.GetByIdsAsync(request.TagIds, userId, cancellationToken);
            foreach (var tag in tags)
                task.TaskTags.Add(new TaskTag { TaskId = task.Id, TagId = tag.Id });
        }

        foreach (var log in changes)
            task.ActivityLogs.Add(log);

        await taskRepository.SaveChangesAsync(cancellationToken);
        return MapToResponse(task);
    }

    public async Task<TaskResponse?> PatchTaskAsync(Guid id, PatchTaskRequest request, string userId, string modifiedBy, CancellationToken cancellationToken = default)
    {
        var task = await taskRepository.GetByIdWithTagsAsync(id, userId, cancellationToken);
        if (task is null) return null;

        if (request.Title is not null) task.Title = request.Title;
        if (request.Description is not null) task.Description = request.Description;
        if (request.Type is not null) task.Type = request.Type;
        if (request.Priority.HasValue) task.Priority = request.Priority.Value;
        if (request.Status.HasValue) task.Status = request.Status.Value;
        if (request.TargetDateType.HasValue) task.TargetDateType = request.TargetDateType.Value;
        if (request.TargetDate.HasValue) task.TargetDate = request.TargetDate.Value;
        if (request.IsRecurring.HasValue) task.IsRecurring = request.IsRecurring.Value;
        if (request.RecurrencePattern.HasValue) task.RecurrencePattern = request.RecurrencePattern.Value;
        task.LastModifiedBy = modifiedBy;

        if (request.TagIds is not null)
        {
            task.TaskTags.Clear();
            if (request.TagIds.Count > 0)
            {
                var tags = await tagRepository.GetByIdsAsync(request.TagIds, userId, cancellationToken);
                foreach (var tag in tags)
                    task.TaskTags.Add(new TaskTag { TaskId = task.Id, TagId = tag.Id });
            }
        }

        task.ActivityLogs.Add(new TaskActivityLog
        {
            TaskId = task.Id,
            Timestamp = DateTime.UtcNow,
            FieldChanged = "patch",
            ChangedBy = modifiedBy
        });

        await taskRepository.SaveChangesAsync(cancellationToken);
        return MapToResponse(task);
    }

    public async Task<TaskResponse?> CompleteTaskAsync(Guid id, CompleteTaskRequest request, string userId, string modifiedBy, CancellationToken cancellationToken = default)
    {
        var task = await taskRepository.GetByIdWithTagsAsync(id, userId, cancellationToken);
        if (task is null) return null;

        task.Status = Shared.Enums.TaskStatus.Completed;
        task.CompletedDate = DateTime.UtcNow;
        task.ResultAnalysis = request.ResultAnalysis;
        task.LastModifiedBy = modifiedBy;

        task.ActivityLogs.Add(new TaskActivityLog
        {
            TaskId = task.Id,
            Timestamp = DateTime.UtcNow,
            FieldChanged = nameof(task.Status),
            OldValue = task.Status.ToString(),
            NewValue = Shared.Enums.TaskStatus.Completed.ToString(),
            ChangedBy = modifiedBy
        });

        if (task.IsRecurring && task.RecurrencePattern.HasValue)
            await CreateRecurringSuccessorAsync(task, userId, modifiedBy, cancellationToken);

        await taskRepository.SaveChangesAsync(cancellationToken);
        return MapToResponse(task);
    }

    public async Task<bool> DeleteTaskAsync(Guid id, string userId, string modifiedBy, CancellationToken cancellationToken = default)
    {
        var task = await taskRepository.GetByIdAsync(id, cancellationToken);
        if (task is null || task.UserId != userId) return false;

        task.IsDeleted = true;
        task.DeletedAt = DateTime.UtcNow;
        task.LastModifiedBy = modifiedBy;

        await taskRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UpdateSortOrderAsync(Guid id, int sortOrder, string userId, CancellationToken cancellationToken = default)
    {
        var task = await taskRepository.GetByIdAsync(id, cancellationToken);
        if (task is null || task.UserId != userId) return false;

        task.SortOrder = sortOrder;
        await taskRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task CreateRecurringSuccessorAsync(TaskItem source, string userId, string modifiedBy, CancellationToken cancellationToken)
    {
        var nextTargetDate = source.RecurrencePattern switch
        {
            RecurrencePattern.Daily => source.TargetDate?.AddDays(1),
            RecurrencePattern.Weekly => source.TargetDate?.AddDays(7),
            RecurrencePattern.Monthly => source.TargetDate?.AddMonths(1),
            _ => null
        };

        var successor = new TaskItem
        {
            Title = source.Title,
            Description = source.Description,
            Type = source.Type,
            Priority = source.Priority,
            Status = Shared.Enums.TaskStatus.NotStarted,
            TargetDateType = source.TargetDateType,
            TargetDate = nextTargetDate,
            IsRecurring = source.IsRecurring,
            RecurrencePattern = source.RecurrencePattern,
            SortOrder = await taskRepository.GetMaxSortOrderAsync(userId, cancellationToken) + 1,
            UserId = userId,
            LastModifiedBy = modifiedBy,
            TaskTags = source.TaskTags.Select(tt => new TaskTag { TagId = tt.TagId }).ToList()
        };

        await taskRepository.AddAsync(successor, cancellationToken);
    }

    private static List<TaskActivityLog> BuildActivityLogs(TaskItem task, UpdateTaskRequest request, string modifiedBy)
    {
        var logs = new List<TaskActivityLog>();
        var now = DateTime.UtcNow;

        void Log(string field, string? oldVal, string? newVal)
        {
            if (oldVal != newVal)
                logs.Add(new TaskActivityLog { TaskId = task.Id, Timestamp = now, FieldChanged = field, OldValue = oldVal, NewValue = newVal, ChangedBy = modifiedBy });
        }

        Log(nameof(task.Title), task.Title, request.Title);
        Log(nameof(task.Description), task.Description, request.Description);
        Log(nameof(task.Type), task.Type, request.Type);
        Log(nameof(task.Priority), task.Priority.ToString(), request.Priority.ToString());
        Log(nameof(task.Status), task.Status.ToString(), request.Status.ToString());
        Log(nameof(task.TargetDate), task.TargetDate?.ToString("O"), request.TargetDate?.ToString("O"));

        return logs;
    }

    private static TaskResponse MapToResponse(TaskItem task) => new(
        task.Id,
        task.Title,
        task.Description,
        task.Type,
        task.Priority,
        task.Status,
        task.TargetDateType,
        task.TargetDate,
        task.CompletedDate,
        task.ResultAnalysis,
        task.IsRecurring,
        task.RecurrencePattern,
        task.SortOrder,
        task.CreatedDate,
        task.LastModifiedDate,
        task.LastModifiedBy,
        task.UserId,
        task.TaskTags.Select(tt => new TagResponse(tt.Tag.Id, tt.Tag.Name, tt.Tag.Color, tt.Tag.CreatedDate)).ToList()
    );
}
