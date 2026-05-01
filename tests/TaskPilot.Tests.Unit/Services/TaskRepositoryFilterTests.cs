using TaskPilot.Data;
using TaskPilot.Entities;
using TaskPilot.Models.Enums;
using TaskPilot.Models.Tasks;
using TaskPilot.Repositories;
using TaskPilot.Tests.Unit.Helpers;
using TaskStatus = TaskPilot.Models.Enums.TaskStatus;

namespace TaskPilot.Tests.Unit.Services;

/// <summary>
/// Repository-level tests for the new IncludeOnlyIncomplete and OverdueOnly filters
/// added to <see cref="TaskRepository.GetPagedAsync"/>. Lives under the Services
/// namespace to follow the existing convention (<see cref="StatsServiceTests"/>
/// also exercises the repo layer indirectly via in-memory EF).
/// </summary>
public class TaskRepositoryFilterTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TaskRepository _repo;

    public TaskRepositoryFilterTests()
    {
        _context = TestDbContextFactory.Create();
        _repo = new TaskRepository(_context);

        // Seed a TaskType so the Include(t => t.TaskType) in GetPagedAsync resolves
        // a non-null navigation under the in-memory provider.
        _context.TaskTypes.Add(new TaskType { Id = 1, Name = "Task", SortOrder = 1 });
        _context.SaveChanges();
    }

    public void Dispose() => _context.Dispose();

    private TaskItem MakeTask(string userId, TaskStatus status,
        TaskPriority priority = TaskPriority.Medium,
        DateTime? targetDate = null,
        bool isDeleted = false,
        int sortOrder = 0)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = $"task-{Guid.NewGuid()}",
            TaskTypeId = 1,
            Area = Area.Personal,
            Priority = priority,
            Status = status,
            TargetDateType = TargetDateType.ThisWeek,
            TargetDate = targetDate,
            SortOrder = sortOrder,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? DateTime.UtcNow : null,
            UserId = userId,
            LastModifiedBy = "user:test@example.com"
        };

    [Fact]
    public async Task GetPagedAsync_WithIncompleteFilter_ReturnsOnlyNotStartedInProgressBlocked()
    {
        _context.Tasks.AddRange(
            MakeTask("u1", TaskStatus.NotStarted),
            MakeTask("u1", TaskStatus.InProgress),
            MakeTask("u1", TaskStatus.Blocked),
            MakeTask("u1", TaskStatus.Completed),
            MakeTask("u1", TaskStatus.Cancelled));
        await _context.SaveChangesAsync();

        var (items, total) = await _repo.GetPagedAsync(
            new TaskQueryParams(IncludeOnlyIncomplete: true), "u1");

        Assert.Equal(3, total);
        Assert.All(items, t => Assert.Contains(t.Status,
            new[] { TaskStatus.NotStarted, TaskStatus.InProgress, TaskStatus.Blocked }));
    }

    [Fact]
    public async Task GetPagedAsync_WithIncompleteFilter_ExcludesCompleted()
    {
        _context.Tasks.AddRange(
            MakeTask("u1", TaskStatus.Completed),
            MakeTask("u1", TaskStatus.NotStarted));
        await _context.SaveChangesAsync();

        var (items, _) = await _repo.GetPagedAsync(
            new TaskQueryParams(IncludeOnlyIncomplete: true), "u1");

        Assert.DoesNotContain(items, t => t.Status == TaskStatus.Completed);
    }

    [Fact]
    public async Task GetPagedAsync_WithIncompleteFilter_ExcludesCancelled()
    {
        _context.Tasks.AddRange(
            MakeTask("u1", TaskStatus.Cancelled),
            MakeTask("u1", TaskStatus.InProgress));
        await _context.SaveChangesAsync();

        var (items, _) = await _repo.GetPagedAsync(
            new TaskQueryParams(IncludeOnlyIncomplete: true), "u1");

        Assert.DoesNotContain(items, t => t.Status == TaskStatus.Cancelled);
    }

    [Fact]
    public async Task GetPagedAsync_WithIncompleteFilter_ExcludesSoftDeleted()
    {
        _context.Tasks.AddRange(
            MakeTask("u1", TaskStatus.NotStarted, isDeleted: true),
            MakeTask("u1", TaskStatus.NotStarted));
        await _context.SaveChangesAsync();

        var (_, total) = await _repo.GetPagedAsync(
            new TaskQueryParams(IncludeOnlyIncomplete: true), "u1");

        Assert.Equal(1, total);
    }

    [Fact]
    public async Task GetPagedAsync_WithIncompleteFilter_WrongUser_ReturnsEmpty()
    {
        _context.Tasks.AddRange(
            MakeTask("owner", TaskStatus.NotStarted),
            MakeTask("owner", TaskStatus.Blocked));
        await _context.SaveChangesAsync();

        var (items, total) = await _repo.GetPagedAsync(
            new TaskQueryParams(IncludeOnlyIncomplete: true), "intruder");

        Assert.Empty(items);
        Assert.Equal(0, total);
    }

    [Fact]
    public async Task GetPagedAsync_WithOverdueOnly_FiltersToTargetDateInPastAndNotNull()
    {
        var yesterday = DateTime.UtcNow.AddDays(-1);
        var tomorrow = DateTime.UtcNow.AddDays(1);
        _context.Tasks.AddRange(
            MakeTask("u1", TaskStatus.NotStarted, targetDate: yesterday),
            MakeTask("u1", TaskStatus.NotStarted, targetDate: tomorrow),
            MakeTask("u1", TaskStatus.InProgress, targetDate: null));
        await _context.SaveChangesAsync();

        var (items, _) = await _repo.GetPagedAsync(
            new TaskQueryParams(OverdueOnly: true), "u1");

        Assert.Single(items);
        Assert.NotNull(items[0].TargetDate);
        Assert.True(items[0].TargetDate < DateTime.UtcNow);
    }

    [Fact]
    public async Task GetPagedAsync_WithOverdueOnly_ExcludesCompletedAndCancelled()
    {
        // Even with a past target date, Completed and Cancelled tasks are not
        // "overdue" — the spec defines overdue as past-and-incomplete.
        var yesterday = DateTime.UtcNow.AddDays(-1);
        _context.Tasks.AddRange(
            MakeTask("u1", TaskStatus.NotStarted, targetDate: yesterday),  // matches
            MakeTask("u1", TaskStatus.InProgress, targetDate: yesterday),  // matches
            MakeTask("u1", TaskStatus.Blocked,    targetDate: yesterday),  // matches
            MakeTask("u1", TaskStatus.Completed,  targetDate: yesterday),  // EXCLUDED
            MakeTask("u1", TaskStatus.Cancelled,  targetDate: yesterday)); // EXCLUDED
        await _context.SaveChangesAsync();

        var (items, total) = await _repo.GetPagedAsync(
            new TaskQueryParams(OverdueOnly: true), "u1");

        Assert.Equal(3, total);
        Assert.All(items, t => Assert.NotEqual(TaskStatus.Completed, t.Status));
        Assert.All(items, t => Assert.NotEqual(TaskStatus.Cancelled, t.Status));
    }

    [Fact]
    public async Task GetPagedAsync_WithIncompleteAndOverdue_Composes()
    {
        var yesterday = DateTime.UtcNow.AddDays(-1);
        _context.Tasks.AddRange(
            MakeTask("u1", TaskStatus.NotStarted, targetDate: yesterday),  // matches
            MakeTask("u1", TaskStatus.Completed,  targetDate: yesterday),  // overdue but not incomplete
            MakeTask("u1", TaskStatus.Blocked,    targetDate: null));      // incomplete but not overdue
        await _context.SaveChangesAsync();

        var (items, _) = await _repo.GetPagedAsync(
            new TaskQueryParams(IncludeOnlyIncomplete: true, OverdueOnly: true), "u1");

        Assert.Single(items);
        Assert.Equal(TaskStatus.NotStarted, items[0].Status);
    }

    [Fact]
    public async Task GetPagedAsync_WithIncompleteFilter_DefaultSort_PriorityDescThenTargetDateAscNullsLast()
    {
        // Build a deterministic mix:
        //   high priority + null date  → priority wins, but among same priority null sorts last
        //   high priority + earlier date
        //   high priority + later date
        //   low priority  + earliest date (still after the highs because priority desc)
        var highPriEarly = MakeTask("u1", TaskStatus.NotStarted, TaskPriority.High,
            targetDate: DateTime.UtcNow.AddDays(1));
        var highPriLater = MakeTask("u1", TaskStatus.InProgress, TaskPriority.High,
            targetDate: DateTime.UtcNow.AddDays(7));
        var highPriNoDate = MakeTask("u1", TaskStatus.Blocked, TaskPriority.High, targetDate: null);
        var lowPriEarliest = MakeTask("u1", TaskStatus.NotStarted, TaskPriority.Low,
            targetDate: DateTime.UtcNow);

        _context.Tasks.AddRange(highPriNoDate, lowPriEarliest, highPriLater, highPriEarly);
        await _context.SaveChangesAsync();

        var (items, _) = await _repo.GetPagedAsync(
            new TaskQueryParams(IncludeOnlyIncomplete: true), "u1");

        Assert.Equal(highPriEarly.Id, items[0].Id);
        Assert.Equal(highPriLater.Id, items[1].Id);
        Assert.Equal(highPriNoDate.Id, items[2].Id);
        Assert.Equal(lowPriEarliest.Id, items[3].Id);
    }

    [Fact]
    public async Task GetPagedAsync_NoFilters_DoesNotApplyIncompleteOrOverdue()
    {
        // Defaults must remain unchanged for callers that don't set the new flags.
        _context.Tasks.AddRange(
            MakeTask("u1", TaskStatus.NotStarted),
            MakeTask("u1", TaskStatus.Completed),
            MakeTask("u1", TaskStatus.Cancelled));
        await _context.SaveChangesAsync();

        var (items, total) = await _repo.GetPagedAsync(new TaskQueryParams(), "u1");

        Assert.Equal(3, total);
        Assert.Equal(3, items.Count);
    }
}
