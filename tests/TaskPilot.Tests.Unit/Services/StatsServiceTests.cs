using Microsoft.EntityFrameworkCore;
using TaskPilot.Data;
using TaskPilot.Entities;
using TaskPilot.Models.Enums;
using TaskPilot.Services;
using TaskStatus = TaskPilot.Models.Enums.TaskStatus;

namespace TaskPilot.Tests.Unit.Services;

public class StatsServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly StatsService _service;

    public StatsServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new StatsService(_context);
    }

    public void Dispose() => _context.Dispose();

    private TaskItem MakeTask(string userId, TaskStatus status, string type = "Work",
        TaskPriority priority = TaskPriority.Medium, DateTime? targetDate = null,
        DateTime? completedDate = null, DateTime? createdDate = null)
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = "Task",
            Type = type,
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
    public async Task GetTaskStatsAsync_ByType_GroupsByType()
    {
        _context.Tasks.AddRange(
            MakeTask("user1", TaskStatus.NotStarted, type: "Work"),
            MakeTask("user1", TaskStatus.NotStarted, type: "Work"),
            MakeTask("user1", TaskStatus.NotStarted, type: "Personal"),
            MakeTask("user1", TaskStatus.Completed, type: "Work") // excluded from ByType
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetTaskStatsAsync("user1");

        var workType = result.ByType.FirstOrDefault(t => t.Type == "Work");
        var personalType = result.ByType.FirstOrDefault(t => t.Type == "Personal");

        Assert.NotNull(workType);
        Assert.Equal(2, workType.Count);
        Assert.NotNull(personalType);
        Assert.Equal(1, personalType.Count);
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
}
