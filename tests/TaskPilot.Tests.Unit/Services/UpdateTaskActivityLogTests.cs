using Moq;
using TaskPilot.Entities;
using TaskPilot.Repositories.Interfaces;
using TaskPilot.Services;
using TaskPilot.Models.Tasks;
using TaskPilot.Models.Enums;
using TaskStatus = TaskPilot.Models.Enums.TaskStatus;

namespace TaskPilot.Tests.Unit.Services;

/// <summary>
/// Unit tests proving defect fix D-002:
///   UpdateTaskAsync now writes per-field activity-log entries for
///   TargetDateType, IsRecurring, and RecurrencePattern in addition to the
///   previously-covered fields (Title, Description, Area, Priority, Status,
///   TaskTypeId, TargetDate).
///
/// Each test would FAIL if <see cref="TaskService.BuildActivityLogs"/> were
/// reverted to omit those three fields.
///
/// REQ: BIZ-TASKS-007 (full-update logs each changed field).
/// </summary>
public class UpdateTaskActivityLogTests
{
    private readonly Mock<ITaskRepository> _taskRepoMock = new();
    private readonly Mock<ITagRepository> _tagRepoMock = new();
    private readonly TaskService _service;

    public UpdateTaskActivityLogTests()
    {
        _service = new TaskService(_taskRepoMock.Object, _tagRepoMock.Object);
    }

    /// <summary>Builds a fully-populated TaskItem so that none of the "no change"
    /// short-circuits trigger unless we deliberately set old == new.</summary>
    private static TaskItem MakeTask(string userId = "u1") => new()
    {
        Id = Guid.NewGuid(),
        Title = "Old Title",
        Description = null,
        TaskTypeId = 1,
        Area = Area.Personal,
        Priority = TaskPriority.Medium,
        Status = TaskStatus.NotStarted,
        TargetDateType = TargetDateType.ThisWeek,
        TargetDate = null,
        IsRecurring = false,
        RecurrencePattern = null,
        SortOrder = 1,
        UserId = userId,
        LastModifiedBy = "user:old@example.com",
        TaskTags = [],
        ActivityLogs = []
    };

    private void SetupSaveableTask(TaskItem task)
    {
        _taskRepoMock.Setup(r => r.GetByIdWithTagsAsync(task.Id, task.UserId, default))
            .ReturnsAsync(task);
        _tagRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), task.UserId, default))
            .ReturnsAsync([]);
        _taskRepoMock.Setup(r => r.Update(task));
        _taskRepoMock.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);
    }

    // ── TargetDateType ────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateTaskAsync_TargetDateTypeChanged_WritesActivityLog()
    {
        var task = MakeTask();
        task.TargetDateType = TargetDateType.ThisWeek;
        SetupSaveableTask(task);

        var request = new UpdateTaskRequest(
            task.Title, task.Description, task.TaskTypeId, task.Area, task.Priority,
            task.Status, TargetDateType.ThisMonth, task.TargetDate, task.IsRecurring,
            task.RecurrencePattern, null);

        await _service.UpdateTaskAsync(task.Id, request, task.UserId, "user:test@example.com");

        Assert.Contains(task.ActivityLogs, l =>
            l.FieldChanged == nameof(task.TargetDateType)
            && l.OldValue == nameof(TargetDateType.ThisWeek)
            && l.NewValue == nameof(TargetDateType.ThisMonth));
    }

    [Fact]
    public async Task UpdateTaskAsync_TargetDateTypeUnchanged_DoesNotWriteActivityLog()
    {
        var task = MakeTask();
        task.TargetDateType = TargetDateType.ThisWeek;
        SetupSaveableTask(task);

        // Same TargetDateType — should not produce a log entry for that field.
        var request = new UpdateTaskRequest(
            task.Title, task.Description, task.TaskTypeId, task.Area, task.Priority,
            task.Status, TargetDateType.ThisWeek, task.TargetDate, task.IsRecurring,
            task.RecurrencePattern, null);

        await _service.UpdateTaskAsync(task.Id, request, task.UserId, "user:test@example.com");

        Assert.DoesNotContain(task.ActivityLogs,
            l => l.FieldChanged == nameof(task.TargetDateType));
    }

    // ── IsRecurring ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateTaskAsync_IsRecurringChangedToTrue_WritesActivityLog()
    {
        var task = MakeTask();
        task.IsRecurring = false;
        SetupSaveableTask(task);

        var request = new UpdateTaskRequest(
            task.Title, task.Description, task.TaskTypeId, task.Area, task.Priority,
            task.Status, task.TargetDateType, task.TargetDate,
            IsRecurring: true,                 // changed
            RecurrencePattern.Daily, null);

        await _service.UpdateTaskAsync(task.Id, request, task.UserId, "user:test@example.com");

        Assert.Contains(task.ActivityLogs, l =>
            l.FieldChanged == nameof(task.IsRecurring)
            && l.OldValue == false.ToString()
            && l.NewValue == true.ToString());
    }

    [Fact]
    public async Task UpdateTaskAsync_IsRecurringUnchanged_DoesNotWriteActivityLog()
    {
        var task = MakeTask();
        task.IsRecurring = false;
        SetupSaveableTask(task);

        var request = new UpdateTaskRequest(
            task.Title, task.Description, task.TaskTypeId, task.Area, task.Priority,
            task.Status, task.TargetDateType, task.TargetDate,
            IsRecurring: false,                // no change
            task.RecurrencePattern, null);

        await _service.UpdateTaskAsync(task.Id, request, task.UserId, "user:test@example.com");

        Assert.DoesNotContain(task.ActivityLogs,
            l => l.FieldChanged == nameof(task.IsRecurring));
    }

    // ── RecurrencePattern ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateTaskAsync_RecurrencePatternChanged_WritesActivityLog()
    {
        var task = MakeTask();
        task.IsRecurring = true;
        task.RecurrencePattern = RecurrencePattern.Daily;
        SetupSaveableTask(task);

        var request = new UpdateTaskRequest(
            task.Title, task.Description, task.TaskTypeId, task.Area, task.Priority,
            task.Status, task.TargetDateType, task.TargetDate, task.IsRecurring,
            RecurrencePattern.Weekly, null);   // changed Daily → Weekly

        await _service.UpdateTaskAsync(task.Id, request, task.UserId, "user:test@example.com");

        Assert.Contains(task.ActivityLogs, l =>
            l.FieldChanged == nameof(task.RecurrencePattern)
            && l.OldValue == nameof(RecurrencePattern.Daily)
            && l.NewValue == nameof(RecurrencePattern.Weekly));
    }

    [Fact]
    public async Task UpdateTaskAsync_RecurrencePatternFromNullToValue_WritesActivityLog()
    {
        var task = MakeTask();
        task.IsRecurring = false;
        task.RecurrencePattern = null;
        SetupSaveableTask(task);

        var request = new UpdateTaskRequest(
            task.Title, task.Description, task.TaskTypeId, task.Area, task.Priority,
            task.Status, task.TargetDateType, task.TargetDate,
            IsRecurring: true,
            RecurrencePattern.Monthly, null);

        await _service.UpdateTaskAsync(task.Id, request, task.UserId, "user:test@example.com");

        Assert.Contains(task.ActivityLogs, l =>
            l.FieldChanged == nameof(task.RecurrencePattern)
            && l.OldValue == null
            && l.NewValue == nameof(RecurrencePattern.Monthly));
    }

    [Fact]
    public async Task UpdateTaskAsync_RecurrencePatternUnchanged_DoesNotWriteActivityLog()
    {
        var task = MakeTask();
        task.IsRecurring = true;
        task.RecurrencePattern = RecurrencePattern.Daily;
        SetupSaveableTask(task);

        // Same value — no log entry expected for this field.
        var request = new UpdateTaskRequest(
            task.Title, task.Description, task.TaskTypeId, task.Area, task.Priority,
            task.Status, task.TargetDateType, task.TargetDate, task.IsRecurring,
            RecurrencePattern.Daily, null);

        await _service.UpdateTaskAsync(task.Id, request, task.UserId, "user:test@example.com");

        Assert.DoesNotContain(task.ActivityLogs,
            l => l.FieldChanged == nameof(task.RecurrencePattern));
    }

    // ── All three new fields together ─────────────────────────────────────────

    [Fact]
    public async Task UpdateTaskAsync_AllThreeNewFieldsChanged_WritesThreeDistinctLogs()
    {
        var task = MakeTask();
        task.TargetDateType = TargetDateType.ThisWeek;
        task.IsRecurring = false;
        task.RecurrencePattern = null;
        SetupSaveableTask(task);

        var request = new UpdateTaskRequest(
            task.Title,                         // unchanged — no log
            task.Description,
            task.TaskTypeId,
            task.Area,
            task.Priority,
            task.Status,
            TargetDateType.ThisMonth,           // changed
            task.TargetDate,
            IsRecurring: true,                  // changed
            RecurrencePattern.Daily,            // changed (null → Daily)
            null);

        await _service.UpdateTaskAsync(task.Id, request, task.UserId, "user:test@example.com");

        Assert.Contains(task.ActivityLogs, l => l.FieldChanged == nameof(task.TargetDateType));
        Assert.Contains(task.ActivityLogs, l => l.FieldChanged == nameof(task.IsRecurring));
        Assert.Contains(task.ActivityLogs, l => l.FieldChanged == nameof(task.RecurrencePattern));

        // Title was not changed, must NOT appear.
        Assert.DoesNotContain(task.ActivityLogs, l => l.FieldChanged == nameof(task.Title));
    }

    // ── ChangedBy is stamped on every new log entry ───────────────────────────

    [Fact]
    public async Task UpdateTaskAsync_NewFieldLogs_CarryCorrectChangedBy()
    {
        var task = MakeTask();
        task.TargetDateType = TargetDateType.ThisWeek;
        SetupSaveableTask(task);

        const string modifiedBy = "user:qa@example.com";
        var request = new UpdateTaskRequest(
            task.Title, task.Description, task.TaskTypeId, task.Area, task.Priority,
            task.Status, TargetDateType.ThisMonth, task.TargetDate,
            task.IsRecurring, task.RecurrencePattern, null);

        await _service.UpdateTaskAsync(task.Id, request, task.UserId, modifiedBy);

        var log = task.ActivityLogs.Single(l => l.FieldChanged == nameof(task.TargetDateType));
        Assert.Equal(modifiedBy, log.ChangedBy);
    }
}
