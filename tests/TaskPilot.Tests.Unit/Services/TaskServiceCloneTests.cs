using Microsoft.EntityFrameworkCore;
using TaskPilot.Data;
using TaskPilot.Entities;
using TaskPilot.Models.Enums;
using TaskPilot.Models.Tasks;
using TaskPilot.Repositories;
using TaskPilot.Services;
using TaskPilot.Tests.Unit.Helpers;
using TaskStatus = TaskPilot.Models.Enums.TaskStatus;

namespace TaskPilot.Tests.Unit.Services;

/// <summary>
/// Unit tests for <see cref="TaskService.CloneTaskAsync"/>.
/// Uses the shared in-memory <see cref="TestDbContextFactory"/> which produces a
/// model that omits the EF10-incompatible HasDefaultValue annotations and ignores
/// Identity entity types.
/// </summary>
public class TaskServiceCloneTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly TaskService _service;

    public TaskServiceCloneTests()
    {
        _db = TestDbContextFactory.Create();

        // Seed a TaskType so .Include(t => t.TaskType) in repo queries resolves
        // a non-null navigation under the in-memory provider.
        _db.TaskTypes.AddRange(
            new TaskType { Id = 1, Name = "Task", SortOrder = 1 },
            new TaskType { Id = 2, Name = "Goal", SortOrder = 2 },
            new TaskType { Id = 3, Name = "Habit", SortOrder = 3 });
        _db.SaveChanges();

        var taskRepo = new TaskRepository(_db);
        var tagRepo = new TagRepository(_db);
        _service = new TaskService(taskRepo, tagRepo);
    }

    public void Dispose() => _db.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Tag SeedTag(string name = "tag1", string userId = "user1")
    {
        var tag = new Tag { Name = name, Color = "#ff0000", UserId = userId, LastModifiedBy = "user:test" };
        _db.Tags.Add(tag);
        _db.SaveChanges();
        return tag;
    }

    private TaskItem SeedTask(
        string userId = "user1",
        string title = "Source Task",
        TaskStatus status = TaskStatus.NotStarted,
        string? description = null,
        int taskTypeId = 2,
        Area area = Area.Personal,
        TaskPriority priority = TaskPriority.Medium,
        TargetDateType targetDateType = TargetDateType.ThisWeek,
        DateTime? targetDate = null,
        bool isRecurring = false,
        RecurrencePattern? recurrencePattern = null,
        DateTime? completedDate = null,
        string? resultAnalysis = null,
        List<Guid>? tagIds = null,
        int sortOrder = 1)
    {
        var task = new TaskItem
        {
            Title = title,
            Description = description,
            TaskTypeId = taskTypeId,
            Area = area,
            Priority = priority,
            Status = status,
            TargetDateType = targetDateType,
            TargetDate = targetDate,
            IsRecurring = isRecurring,
            RecurrencePattern = recurrencePattern,
            CompletedDate = completedDate,
            ResultAnalysis = resultAnalysis,
            SortOrder = sortOrder,
            UserId = userId,
            LastModifiedBy = "user:test"
        };

        if (tagIds?.Count > 0)
            task.TaskTags = tagIds.Select(id => new TaskTag { TagId = id }).ToList();

        _db.Tasks.Add(task);
        _db.SaveChanges();
        return task;
    }

    // ── U-CL-T-001 to U-CL-T-015: Happy path — field mapping ─────────────────

    [Fact]
    public async Task CloneTaskAsync_HappyPath_ReturnsNonNullResponse()
    {
        var source = SeedTask();
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        Assert.NotNull(result);
    }

    [Fact]
    public async Task CloneTaskAsync_HappyPath_NewIdDiffersFromSource()
    {
        var source = SeedTask();
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        Assert.NotEqual(source.Id, result!.Id);
    }

    [Fact]
    public async Task CloneTaskAsync_HappyPath_DefaultTitleHasCopySuffix()
    {
        var source = SeedTask(title: "Prepare slides");
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        Assert.Equal("Prepare slides (copy)", result!.Title);
    }

    [Fact]
    public async Task CloneTaskAsync_HappyPath_DescriptionCopiedVerbatim()
    {
        var source = SeedTask(description: "Some notes");
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        Assert.Equal("Some notes", result!.Description);
    }

    [Fact]
    public async Task CloneTaskAsync_HappyPath_TaskTypeIdCopiedVerbatim()
    {
        var source = SeedTask(taskTypeId: 3);
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        Assert.Equal(3, result!.TaskTypeId);
    }

    [Fact]
    public async Task CloneTaskAsync_HappyPath_AreaCopiedVerbatim()
    {
        var source = SeedTask(area: Area.Work);
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        Assert.Equal(Area.Work, result!.Area);
    }

    [Fact]
    public async Task CloneTaskAsync_HappyPath_PriorityCopiedVerbatim()
    {
        var source = SeedTask(priority: TaskPriority.High);
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        Assert.Equal(TaskPriority.High, result!.Priority);
    }

    [Fact]
    public async Task CloneTaskAsync_HappyPath_StatusForcedToNotStarted()
    {
        var source = SeedTask(status: TaskStatus.InProgress);
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        Assert.Equal(TaskStatus.NotStarted, result!.Status);
    }

    [Fact]
    public async Task CloneTaskAsync_SourceCompleted_StatusForcedToNotStarted()
    {
        var source = SeedTask(status: TaskStatus.Completed, completedDate: DateTime.UtcNow);
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        Assert.Equal(TaskStatus.NotStarted, result!.Status);
    }

    [Fact]
    public async Task CloneTaskAsync_SourceCancelled_StatusForcedToNotStarted()
    {
        var source = SeedTask(status: TaskStatus.Cancelled);
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        Assert.Equal(TaskStatus.NotStarted, result!.Status);
    }

    [Fact]
    public async Task CloneTaskAsync_HappyPath_CompletedDateIsNull()
    {
        var source = SeedTask(completedDate: DateTime.UtcNow);
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        Assert.Null(result!.CompletedDate);
    }

    [Fact]
    public async Task CloneTaskAsync_HappyPath_ResultAnalysisIsNull()
    {
        var source = SeedTask(resultAnalysis: "Went well");
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        Assert.Null(result!.ResultAnalysis);
    }

    [Fact]
    public async Task CloneTaskAsync_HappyPath_IsDeletedFalseOnClone()
    {
        var source = SeedTask();
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        var clone = await _db.Tasks.IgnoreQueryFilters().FirstAsync(t => t.Id == result!.Id);
        Assert.False(clone.IsDeleted);
    }

    [Fact]
    public async Task CloneTaskAsync_HappyPath_RecurrencePatternCopied()
    {
        var source = SeedTask(isRecurring: true, recurrencePattern: RecurrencePattern.Weekly);
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        Assert.True(result!.IsRecurring);
        Assert.Equal(RecurrencePattern.Weekly, result.RecurrencePattern);
    }

    [Fact]
    public async Task CloneTaskAsync_SourceNotRecurring_RecurrencePatternNullOnClone()
    {
        var source = SeedTask(isRecurring: false, recurrencePattern: null);
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        Assert.False(result!.IsRecurring);
        Assert.Null(result.RecurrencePattern);
    }

    // ── U-CL-T-016 to U-CL-T-021: Target-date handling ───────────────────────

    [Fact]
    public async Task CloneTaskAsync_NeitherOverrideNorClear_TargetDateCopiedVerbatim()
    {
        var may30 = new DateTime(2026, 5, 30, 0, 0, 0, DateTimeKind.Utc);
        var source = SeedTask(targetDate: may30);
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        Assert.Equal(may30, result!.TargetDate);
    }

    [Fact]
    public async Task CloneTaskAsync_TargetDateOverride_UsesOverrideDate()
    {
        var may30 = new DateTime(2026, 5, 30, 0, 0, 0, DateTimeKind.Utc);
        var june15 = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var source = SeedTask(targetDate: may30);
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(TargetDate: june15, ClearTargetDate: false), "user1", "user:alice");
        Assert.Equal(june15, result!.TargetDate);
    }

    [Fact]
    public async Task CloneTaskAsync_ClearTargetDateTrue_TargetDateIsNull()
    {
        var may30 = new DateTime(2026, 5, 30, 0, 0, 0, DateTimeKind.Utc);
        var source = SeedTask(targetDate: may30);
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(ClearTargetDate: true), "user1", "user:alice");
        Assert.Null(result!.TargetDate);
    }

    [Fact]
    public async Task CloneTaskAsync_ClearTargetDateTrueWithOverride_OverrideIgnoredDateIsNull()
    {
        var may30 = new DateTime(2026, 5, 30, 0, 0, 0, DateTimeKind.Utc);
        var june15 = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var source = SeedTask(targetDate: may30);
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(TargetDate: june15, ClearTargetDate: true), "user1", "user:alice");
        Assert.Null(result!.TargetDate);
    }

    [Fact]
    public async Task CloneTaskAsync_SourceHasNoTargetDate_CloneAlsoHasNone()
    {
        var source = SeedTask(targetDate: null);
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        Assert.Null(result!.TargetDate);
    }

    [Fact]
    public async Task CloneTaskAsync_TargetDateTypeCopiedVerbatim()
    {
        var source = SeedTask(targetDateType: TargetDateType.ThisWeek);
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        Assert.Equal(TargetDateType.ThisWeek, result!.TargetDateType);
    }

    // ── U-CL-T-022 to U-CL-T-026: Title override ─────────────────────────────

    [Fact]
    public async Task CloneTaskAsync_TitleOverrideSupplied_UsesOverride()
    {
        var source = SeedTask(title: "Foo");
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(Title: "Bar"), "user1", "user:alice");
        Assert.Equal("Bar", result!.Title);
    }

    [Fact]
    public async Task CloneTaskAsync_TitleOverrideNull_UsesCopySuffix()
    {
        var source = SeedTask(title: "Foo");
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(Title: null), "user1", "user:alice");
        Assert.Equal("Foo (copy)", result!.Title);
    }

    [Fact]
    public async Task CloneTaskAsync_TitleOverrideWhitespaceOnly_UsesCopySuffix()
    {
        var source = SeedTask(title: "Foo");
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(Title: "   "), "user1", "user:alice");
        Assert.Equal("Foo (copy)", result!.Title);
    }

    [Fact]
    public async Task CloneTaskAsync_TitleOverrideEmptyString_UsesCopySuffix()
    {
        var source = SeedTask(title: "Foo");
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(Title: ""), "user1", "user:alice");
        Assert.Equal("Foo (copy)", result!.Title);
    }

    [Fact]
    public async Task CloneTaskAsync_DoubleClone_ProducesCopyCopySuffix()
    {
        var source = SeedTask(title: "Foo (copy)");
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        Assert.Equal("Foo (copy) (copy)", result!.Title);
    }

    [Fact]
    public async Task CloneTaskAsync_MaxLengthSource_CloneTitleFitsLimit()
    {
        var source = SeedTask(title: new string('A', 200));
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        Assert.True(result!.Title.Length <= 200, $"Clone title was {result.Title.Length} chars, expected <= 200");
        Assert.EndsWith(" (copy)", result.Title);
    }

    [Fact]
    public async Task CloneTaskAsync_TitleOverrideTooLong_TruncatedToLimit()
    {
        // Belt-and-braces against the MCP path, which bypasses CloneTaskRequestValidator.
        var source = SeedTask(title: "Source");
        var oversized = new string('B', 250);
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(Title: oversized), "user1", "user:alice");
        Assert.Equal(200, result!.Title.Length);
        Assert.StartsWith("B", result.Title);
    }

    // ── U-CL-T-027 to U-CL-T-029: Tags ──────────────────────────────────────

    [Fact]
    public async Task CloneTaskAsync_TagsCopied_CountMatchesSource()
    {
        var t1 = SeedTag("tag1"); var t2 = SeedTag("tag2"); var t3 = SeedTag("tag3");
        var source = SeedTask(tagIds: [t1.Id, t2.Id, t3.Id]);
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        var cloneTags = await _db.TaskTags.Where(tt => tt.TaskId == result!.Id).ToListAsync();
        Assert.Equal(3, cloneTags.Count);
    }

    [Fact]
    public async Task CloneTaskAsync_TagsCopied_TagIdsMatchSource()
    {
        var t1 = SeedTag("tagA"); var t2 = SeedTag("tagB");
        var source = SeedTask(tagIds: [t1.Id, t2.Id]);
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        var cloneTagIds = await _db.TaskTags.Where(tt => tt.TaskId == result!.Id).Select(tt => tt.TagId).ToListAsync();
        Assert.Contains(t1.Id, cloneTagIds);
        Assert.Contains(t2.Id, cloneTagIds);
    }

    [Fact]
    public async Task CloneTaskAsync_SourceHasNoTags_CloneHasNoTags()
    {
        var source = SeedTask();
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        var cloneTags = await _db.TaskTags.Where(tt => tt.TaskId == result!.Id).ToListAsync();
        Assert.Empty(cloneTags);
    }

    // ── U-CL-T-030 to U-CL-T-031: SortOrder ─────────────────────────────────

    [Fact]
    public async Task CloneTaskAsync_SortOrderIsMaxPlusOne()
    {
        SeedTask(sortOrder: 1); SeedTask(sortOrder: 2, title: "T2"); SeedTask(sortOrder: 3, title: "T3");
        var source = SeedTask(sortOrder: 4, title: "T4");
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        Assert.Equal(5, result!.SortOrder);
    }

    [Fact]
    public async Task CloneTaskAsync_NoExistingTasks_SortOrderIsOne()
    {
        // fresh DB — seed directly so no prior tasks exist
        var task = new TaskItem
        {
            Title = "Only Task", TaskTypeId = 1, Area = Area.Personal, Priority = TaskPriority.Medium,
            Status = TaskStatus.NotStarted, TargetDateType = TargetDateType.ThisWeek,
            SortOrder = 1, UserId = "userX", LastModifiedBy = "user:x"
        };
        _db.Tasks.Add(task);
        _db.SaveChanges();
        var result = await _service.CloneTaskAsync(task.Id, new CloneTaskRequest(), "userX", "user:x");
        Assert.Equal(2, result!.SortOrder);
    }

    // ── U-CL-T-032 to U-CL-T-037: ActivityLog ────────────────────────────────

    [Fact]
    public async Task CloneTaskAsync_ActivityLog_ExactlyOneEntryOnClone()
    {
        var source = SeedTask();
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        var logs = await _db.TaskActivityLogs.Where(l => l.TaskId == result!.Id).ToListAsync();
        Assert.Single(logs);
    }

    [Fact]
    public async Task CloneTaskAsync_ActivityLog_FieldChangedIsCreated()
    {
        var source = SeedTask();
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        var log = await _db.TaskActivityLogs.FirstAsync(l => l.TaskId == result!.Id);
        Assert.Equal("Created", log.FieldChanged);
    }

    [Fact]
    public async Task CloneTaskAsync_ActivityLog_NewValueContainsSourceId()
    {
        var source = SeedTask();
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        var log = await _db.TaskActivityLogs.FirstAsync(l => l.TaskId == result!.Id);
        Assert.Equal($"Cloned from {source.Id:D}", log.NewValue);
    }

    [Fact]
    public async Task CloneTaskAsync_ActivityLog_OldValueIsNull()
    {
        var source = SeedTask();
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        var log = await _db.TaskActivityLogs.FirstAsync(l => l.TaskId == result!.Id);
        Assert.Null(log.OldValue);
    }

    [Fact]
    public async Task CloneTaskAsync_ActivityLog_ChangedByIsModifiedBy()
    {
        var source = SeedTask();
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        var log = await _db.TaskActivityLogs.FirstAsync(l => l.TaskId == result!.Id);
        Assert.Equal("user:alice", log.ChangedBy);
    }

    [Fact]
    public async Task CloneTaskAsync_ActivityLog_ApiCaller_ChangedByIsApiKeyName()
    {
        var source = SeedTask();
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "api:Claude-Work");
        var log = await _db.TaskActivityLogs.FirstAsync(l => l.TaskId == result!.Id);
        Assert.Equal("api:Claude-Work", log.ChangedBy);
    }

    // ── U-CL-T-038 to U-CL-T-040: Source immutability ────────────────────────

    [Fact]
    public async Task CloneTaskAsync_SourceTask_NoActivityLogAdded()
    {
        var source = SeedTask();
        // Seed one pre-existing log on the source
        _db.TaskActivityLogs.Add(new TaskActivityLog { TaskId = source.Id, FieldChanged = "Created", NewValue = source.Title, ChangedBy = "user:test", Timestamp = DateTime.UtcNow });
        _db.SaveChanges();

        await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");

        var sourceLogs = await _db.TaskActivityLogs.Where(l => l.TaskId == source.Id).ToListAsync();
        Assert.Single(sourceLogs);
    }

    [Fact]
    public async Task CloneTaskAsync_SourceTask_LastModifiedDateUnchanged()
    {
        var source = SeedTask();
        var originalModified = source.LastModifiedDate;
        await Task.Delay(10); // ensure clock moves slightly
        await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        var refreshed = await _db.Tasks.IgnoreQueryFilters().FirstAsync(t => t.Id == source.Id);
        Assert.Equal(originalModified, refreshed.LastModifiedDate);
    }

    [Fact]
    public async Task CloneTaskAsync_SourceTask_StatusUnchanged()
    {
        var source = SeedTask(status: TaskStatus.Completed);
        await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        var refreshed = await _db.Tasks.IgnoreQueryFilters().FirstAsync(t => t.Id == source.Id);
        Assert.Equal(TaskStatus.Completed, refreshed.Status);
    }

    // ── U-CL-T-041 to U-CL-T-043: 404 / authorization ───────────────────────

    [Fact]
    public async Task CloneTaskAsync_MissingId_ReturnsNull()
    {
        var result = await _service.CloneTaskAsync(Guid.NewGuid(), new CloneTaskRequest(), "user1", "user:alice");
        Assert.Null(result);
    }

    [Fact]
    public async Task CloneTaskAsync_SoftDeletedSource_ReturnsNull()
    {
        var source = SeedTask();
        // Soft-delete it directly
        source.IsDeleted = true;
        source.DeletedAt = DateTime.UtcNow;
        _db.SaveChanges();

        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        Assert.Null(result);
    }

    [Fact]
    public async Task CloneTaskAsync_CrossUserSource_ReturnsNull()
    {
        var source = SeedTask(userId: "userA");
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "userB", "user:bob");
        Assert.Null(result);
    }

    // ── U-CL-T-044 to U-CL-T-045: LastModifiedBy ─────────────────────────────

    [Fact]
    public async Task CloneTaskAsync_LastModifiedBy_UserCaller_FormattedCorrectly()
    {
        var source = SeedTask();
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "user:alice");
        Assert.Equal("user:alice", result!.LastModifiedBy);
    }

    [Fact]
    public async Task CloneTaskAsync_LastModifiedBy_ApiKeyCaller_FormattedCorrectly()
    {
        var source = SeedTask();
        var result = await _service.CloneTaskAsync(source.Id, new CloneTaskRequest(), "user1", "api:MyKey");
        Assert.Equal("api:MyKey", result!.LastModifiedBy);
    }

}
