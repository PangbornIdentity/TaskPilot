using TaskPilot.Data;
using TaskPilot.Entities;
using TaskPilot.Models.Enums;
using TaskPilot.Services;
using TaskPilot.Tests.Unit.Helpers;
using TaskStatus = TaskPilot.Models.Enums.TaskStatus;

namespace TaskPilot.Tests.Unit.Services;

public class StatsServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly StatsService _service;

    public StatsServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _service = new StatsService(_context);
    }

    public void Dispose() => _context.Dispose();

    private TaskItem MakeTask(string userId, TaskStatus status,
        TaskPriority priority = TaskPriority.Medium, DateTime? targetDate = null,
        DateTime? completedDate = null, DateTime? createdDate = null,
        Area area = Area.Personal, int taskTypeId = 1)
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Task",
            TaskTypeId = taskTypeId,
            Area = area,
            Priority = priority,
            Status = status,
            TargetDateType = TargetDateType.ThisWeek,
            TargetDate = targetDate,
            CompletedDate = completedDate,
            SortOrder = 1,
            UserId = userId,
            LastModifiedBy = "user:test@example.com"
        };
        if (createdDate.HasValue) task.CreatedDate = createdDate.Value;
        return task;
    }

    [Fact]
    public async Task GetTaskStatsAsync_TotalActive_CountsNonCompletedNonCancelled()
    {
        _context.Tasks.AddRange(
            MakeTask("user1", TaskStatus.NotStarted),
            MakeTask("user1", TaskStatus.InProgress),
            MakeTask("user1", TaskStatus.Completed),
            MakeTask("user1", TaskStatus.Cancelled)
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetTaskStatsAsync("user1");

        Assert.Equal(2, result.TotalActive);
    }

    [Fact]
    public async Task GetTaskStatsAsync_TotalActive_ExcludesOtherUsers()
    {
        _context.Tasks.AddRange(
            MakeTask("user1", TaskStatus.NotStarted),
            MakeTask("user2", TaskStatus.NotStarted)
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetTaskStatsAsync("user1");

        Assert.Equal(1, result.TotalActive);
    }

    [Fact]
    public async Task GetTaskStatsAsync_CompletedToday_CountsTasksCompletedToday()
    {
        var now = DateTime.UtcNow;
        _context.Tasks.AddRange(
            MakeTask("user1", TaskStatus.Completed, completedDate: now),
            MakeTask("user1", TaskStatus.Completed, completedDate: now.AddHours(-1)),
            MakeTask("user1", TaskStatus.Completed, completedDate: now.AddDays(-2))
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetTaskStatsAsync("user1");

        Assert.Equal(2, result.CompletedToday);
    }

    [Fact]
    public async Task GetTaskStatsAsync_Overdue_CountsTasksPastTargetDate()
    {
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var tomorrow = DateTime.UtcNow.Date.AddDays(1);
        _context.Tasks.AddRange(
            MakeTask("user1", TaskStatus.NotStarted, targetDate: yesterday),   // overdue
            MakeTask("user1", TaskStatus.NotStarted, targetDate: tomorrow),    // not overdue
            MakeTask("user1", TaskStatus.Completed, targetDate: yesterday)     // completed, not overdue
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetTaskStatsAsync("user1");

        Assert.Equal(1, result.Overdue);
    }

    [Fact]
    public async Task GetTaskStatsAsync_InProgress_CountsInProgressOnly()
    {
        _context.Tasks.AddRange(
            MakeTask("user1", TaskStatus.InProgress),
            MakeTask("user1", TaskStatus.InProgress),
            MakeTask("user1", TaskStatus.NotStarted)
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetTaskStatsAsync("user1");

        Assert.Equal(2, result.InProgress);
    }

    [Fact]
    public async Task GetTaskStatsAsync_Blocked_CountsBlockedOnly()
    {
        _context.Tasks.AddRange(
            MakeTask("user1", TaskStatus.Blocked),
            MakeTask("user1", TaskStatus.InProgress)
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetTaskStatsAsync("user1");

        Assert.Equal(1, result.Blocked);
    }

    [Fact]
    public async Task GetTaskStatsAsync_ByType_GroupsByTaskTypeName()
    {
        // Add TaskType entities
        var taskType1 = new TaskType { Id = 101, Name = "Goal", SortOrder = 1 };
        var taskType2 = new TaskType { Id = 102, Name = "Habit", SortOrder = 2 };
        _context.TaskTypes.AddRange(taskType1, taskType2);
        _context.Tasks.AddRange(
            MakeTask("user1", TaskStatus.NotStarted, taskTypeId: 101),
            MakeTask("user1", TaskStatus.NotStarted, taskTypeId: 101),
            MakeTask("user1", TaskStatus.NotStarted, taskTypeId: 102),
            MakeTask("user1", TaskStatus.Completed, taskTypeId: 101) // excluded from ByType
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetTaskStatsAsync("user1");

        var goalType = result.ByType.FirstOrDefault(t => t.Type == "Goal");
        var habitType = result.ByType.FirstOrDefault(t => t.Type == "Habit");

        Assert.NotNull(goalType);
        Assert.Equal(2, goalType.Count);
        Assert.NotNull(habitType);
        Assert.Equal(1, habitType.Count);
    }

    [Fact]
    public async Task GetTaskStatsAsync_ByPriority_GroupsByPriority()
    {
        _context.Tasks.AddRange(
            MakeTask("user1", TaskStatus.NotStarted, priority: TaskPriority.High),
            MakeTask("user1", TaskStatus.InProgress, priority: TaskPriority.High),
            MakeTask("user1", TaskStatus.NotStarted, priority: TaskPriority.Low)
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetTaskStatsAsync("user1");

        var highPriority = result.ByPriority.FirstOrDefault(p => p.Priority == "High");
        Assert.NotNull(highPriority);
        Assert.Equal(1, highPriority.NotStarted);
        Assert.Equal(1, highPriority.InProgress);
    }

    [Fact]
    public async Task GetTaskStatsAsync_CompletedPerWeek_IncludesRecentCompletions()
    {
        var recentDate = DateTime.UtcNow.AddDays(-3);
        _context.Tasks.Add(MakeTask("user1", TaskStatus.Completed, completedDate: recentDate));
        await _context.SaveChangesAsync();

        var result = await _service.GetTaskStatsAsync("user1");

        Assert.NotEmpty(result.CompletedPerWeek);
        Assert.True(result.CompletedPerWeek.Sum(w => w.Count) >= 1);
    }

    [Fact]
    public async Task GetTaskStatsAsync_EmptyDb_ReturnsZeroCounts()
    {
        var result = await _service.GetTaskStatsAsync("user1");

        Assert.Equal(0, result.TotalActive);
        Assert.Equal(0, result.CompletedToday);
        Assert.Equal(0, result.Overdue);
        Assert.Equal(0, result.InProgress);
        Assert.Equal(0, result.Blocked);
    }

    [Fact]
    public async Task GetTaskStatsAsync_CompletionsByArea_CountsCompletedByArea()
    {
        _context.Tasks.AddRange(
            MakeTask("user1", TaskStatus.Completed, area: Area.Personal),
            MakeTask("user1", TaskStatus.Completed, area: Area.Personal),
            MakeTask("user1", TaskStatus.Completed, area: Area.Work),
            MakeTask("user1", TaskStatus.NotStarted, area: Area.Work) // not completed
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetTaskStatsAsync("user1");

        Assert.Equal(2, result.CompletionsByArea.Personal);
        Assert.Equal(1, result.CompletionsByArea.Work);
    }

    // ── New tests — CompletionsByArea and TopTags ──────────────────────────

    [Fact]
    public async Task GetTaskStatsAsync_CompletionsByArea_CountsPersonalAndWorkSeparately()
    {
        _context.Tasks.AddRange(
            MakeTask("user1", TaskStatus.Completed, area: Area.Personal),
            MakeTask("user1", TaskStatus.Completed, area: Area.Personal),
            MakeTask("user1", TaskStatus.Completed, area: Area.Personal),
            MakeTask("user1", TaskStatus.Completed, area: Area.Work),
            MakeTask("user1", TaskStatus.Completed, area: Area.Work),
            MakeTask("user1", TaskStatus.NotStarted, area: Area.Personal) // not completed
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetTaskStatsAsync("user1");

        Assert.Equal(3, result.CompletionsByArea.Personal);
        Assert.Equal(2, result.CompletionsByArea.Work);
    }

    [Fact]
    public async Task GetTaskStatsAsync_TopTags_ReturnsTopFiveByTaskCount()
    {
        // Create 6 tags; tasks reference them with varying frequencies
        var tagA = new Tag { Id = Guid.NewGuid(), Name = "alpha",  Color = "#000001", UserId = "user1" };
        var tagB = new Tag { Id = Guid.NewGuid(), Name = "beta",   Color = "#000002", UserId = "user1" };
        var tagC = new Tag { Id = Guid.NewGuid(), Name = "gamma",  Color = "#000003", UserId = "user1" };
        var tagD = new Tag { Id = Guid.NewGuid(), Name = "delta",  Color = "#000004", UserId = "user1" };
        var tagE = new Tag { Id = Guid.NewGuid(), Name = "epsilon",Color = "#000005", UserId = "user1" };
        var tagF = new Tag { Id = Guid.NewGuid(), Name = "zeta",   Color = "#000006", UserId = "user1" };
        _context.Tags.AddRange(tagA, tagB, tagC, tagD, tagE, tagF);

        // alpha: 6 tasks, beta: 5, gamma: 4, delta: 3, epsilon: 2, zeta: 1
        for (var i = 0; i < 6; i++) AddTaskWithTag("user1", tagA);
        for (var i = 0; i < 5; i++) AddTaskWithTag("user1", tagB);
        for (var i = 0; i < 4; i++) AddTaskWithTag("user1", tagC);
        for (var i = 0; i < 3; i++) AddTaskWithTag("user1", tagD);
        for (var i = 0; i < 2; i++) AddTaskWithTag("user1", tagE);
        for (var i = 0; i < 1; i++) AddTaskWithTag("user1", tagF);

        await _context.SaveChangesAsync();

        var result = await _service.GetTaskStatsAsync("user1");

        // TopTags returns at most 5, ordered by count descending
        Assert.Equal(5, result.TopTags.Count);
        Assert.Equal("alpha",   result.TopTags[0].TagName);
        Assert.Equal(6,         result.TopTags[0].TaskCount);
        Assert.Equal("beta",    result.TopTags[1].TagName);
        Assert.Equal(5,         result.TopTags[1].TaskCount);
        // zeta (count 1) must be excluded
        Assert.DoesNotContain(result.TopTags, t => t.TagName == "zeta");
    }

    private void AddTaskWithTag(string userId, Tag tag)
    {
        var task = MakeTask(userId, TaskStatus.NotStarted);
        task.TaskTags = [new TaskTag { TagId = tag.Id, Tag = tag }];
        _context.Tasks.Add(task);
    }
}
