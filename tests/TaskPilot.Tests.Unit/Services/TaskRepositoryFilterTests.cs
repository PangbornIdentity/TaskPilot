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

    // ───────── Sortable column headers (PR B v1.11) ─────────

    private TaskItem MakeTaskWithTitle(string userId, string title)
    {
        var t = MakeTask(userId, TaskStatus.NotStarted);
        t.Title = title;
        return t;
    }

    [Fact]
    public async Task GetPagedAsync_SortByTitleAsc_OrdersAlphabetically()
    {
        _context.Tasks.AddRange(
            MakeTaskWithTitle("u1", "charlie"),
            MakeTaskWithTitle("u1", "alpha"),
            MakeTaskWithTitle("u1", "bravo"));
        await _context.SaveChangesAsync();

        var (items, _) = await _repo.GetPagedAsync(
            new TaskQueryParams(SortBy: "title", SortDir: "asc"), "u1");

        Assert.Equal(new[] { "alpha", "bravo", "charlie" }, items.Select(t => t.Title).ToArray());
    }

    [Fact]
    public async Task GetPagedAsync_SortByTitleDesc_ReversesOrder()
    {
        _context.Tasks.AddRange(
            MakeTaskWithTitle("u1", "alpha"),
            MakeTaskWithTitle("u1", "charlie"),
            MakeTaskWithTitle("u1", "bravo"));
        await _context.SaveChangesAsync();

        var (items, _) = await _repo.GetPagedAsync(
            new TaskQueryParams(SortBy: "title", SortDir: "desc"), "u1");

        Assert.Equal(new[] { "charlie", "bravo", "alpha" }, items.Select(t => t.Title).ToArray());
    }

    [Fact]
    public async Task GetPagedAsync_SortByStatusAsc_OrdersByEnumValue()
    {
        // Status enum: NotStarted=0, InProgress=1, Blocked=2, Completed=3, Cancelled=4.
        _context.Tasks.AddRange(
            MakeTask("u1", TaskStatus.Cancelled),
            MakeTask("u1", TaskStatus.NotStarted),
            MakeTask("u1", TaskStatus.Blocked),
            MakeTask("u1", TaskStatus.InProgress),
            MakeTask("u1", TaskStatus.Completed));
        await _context.SaveChangesAsync();

        var (items, _) = await _repo.GetPagedAsync(
            new TaskQueryParams(SortBy: "status", SortDir: "asc"), "u1");

        Assert.Equal(
            new[] { TaskStatus.NotStarted, TaskStatus.InProgress, TaskStatus.Blocked, TaskStatus.Completed, TaskStatus.Cancelled },
            items.Select(t => t.Status).ToArray());
    }

    [Fact]
    public async Task GetPagedAsync_SortByAreaAsc_OrdersByEnumValue()
    {
        var work = MakeTask("u1", TaskStatus.NotStarted); work.Area = Area.Work;
        var personal = MakeTask("u1", TaskStatus.NotStarted); personal.Area = Area.Personal;
        _context.Tasks.AddRange(work, personal);
        await _context.SaveChangesAsync();

        var (items, _) = await _repo.GetPagedAsync(
            new TaskQueryParams(SortBy: "area", SortDir: "asc"), "u1");

        Assert.Equal(new[] { Area.Personal, Area.Work }, items.Select(t => t.Area).ToArray());
    }

    [Fact]
    public async Task GetPagedAsync_SortByTypeAsc_OrdersByTaskTypeName()
    {
        var goal  = new TaskType { Id = 201, Name = "Goal",  SortOrder = 1, IsActive = true };
        var habit = new TaskType { Id = 202, Name = "Habit", SortOrder = 2, IsActive = true };
        var task  = new TaskType { Id = 203, Name = "Task",  SortOrder = 3, IsActive = true };
        _context.TaskTypes.AddRange(goal, habit, task);

        var t1 = MakeTask("u1", TaskStatus.NotStarted); t1.TaskTypeId = task.Id;
        var t2 = MakeTask("u1", TaskStatus.NotStarted); t2.TaskTypeId = goal.Id;
        var t3 = MakeTask("u1", TaskStatus.NotStarted); t3.TaskTypeId = habit.Id;
        _context.Tasks.AddRange(t1, t2, t3);
        await _context.SaveChangesAsync();

        var (items, _) = await _repo.GetPagedAsync(
            new TaskQueryParams(SortBy: "type", SortDir: "asc"), "u1");

        Assert.Equal(new[] { goal.Id, habit.Id, task.Id }, items.Select(t => t.TaskTypeId).ToArray());
    }

    [Fact]
    public async Task GetPagedAsync_SortByTargetDate_NullsAlwaysLast()
    {
        // Spec: when sorting by targetdate, null values must sort LAST regardless of direction —
        // a task with no due date is "least urgent" at both ends of the list.
        var future = MakeTask("u1", TaskStatus.NotStarted, targetDate: DateTime.UtcNow.AddDays(7));
        var past   = MakeTask("u1", TaskStatus.NotStarted, targetDate: DateTime.UtcNow.AddDays(-7));
        var noDate = MakeTask("u1", TaskStatus.NotStarted, targetDate: null);
        _context.Tasks.AddRange(future, past, noDate);
        await _context.SaveChangesAsync();

        var (asc, _)  = await _repo.GetPagedAsync(new TaskQueryParams(SortBy: "targetdate", SortDir: "asc"),  "u1");
        var (desc, _) = await _repo.GetPagedAsync(new TaskQueryParams(SortBy: "targetdate", SortDir: "desc"), "u1");

        Assert.Equal(past.Id,   asc[0].Id);
        Assert.Equal(future.Id, asc[1].Id);
        Assert.Null(asc[2].TargetDate);

        Assert.Equal(future.Id, desc[0].Id);
        Assert.Equal(past.Id,   desc[1].Id);
        Assert.Null(desc[2].TargetDate);
    }
}
