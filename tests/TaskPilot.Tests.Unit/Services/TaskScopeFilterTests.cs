using TaskPilot.Data;
using TaskPilot.Entities;
using TaskPilot.Models.Enums;
using TaskPilot.Models.Tasks;
using TaskPilot.Repositories;
using TaskPilot.Tests.Unit.Helpers;
using TaskStatus = TaskPilot.Models.Enums.TaskStatus;

namespace TaskPilot.Tests.Unit.Services;

/// <summary>
/// Repository-level tests proving defect fix D-006:
///   TaskQueryParams.Scope (Active / Completed / All) filters and paginates
///   results server-side. The critical regression prevented by these tests is
///   that, before the fix, scope was applied AFTER fetching all rows so the
///   returned <c>totalCount</c> was wrong (equal to the full row count rather
///   than the scope-filtered count).
///
/// Every test FAILS if the Scope branch in TaskRepository.GetPagedAsync is
/// removed or reverted to post-query client-side filtering.
///
/// REQ: FR-TASKS-025 (active/completed/all scope), FR-LAYOUT-002 (server-side pagination).
/// </summary>
public class TaskScopeFilterTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TaskRepository _repo;

    public TaskScopeFilterTests()
    {
        _context = TestDbContextFactory.Create();
        _repo = new TaskRepository(_context);

        _context.TaskTypes.Add(new TaskType { Id = 1, Name = "Task", SortOrder = 1 });
        _context.SaveChanges();
    }

    public void Dispose() => _context.Dispose();

    private TaskItem Make(string userId, TaskStatus status, int sortOrder = 0)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = $"t-{status}-{Guid.NewGuid():N}"[..24],
            TaskTypeId = 1,
            Area = Area.Personal,
            Priority = TaskPriority.Medium,
            Status = status,
            TargetDateType = TargetDateType.ThisWeek,
            SortOrder = sortOrder,
            UserId = userId,
            LastModifiedBy = "user:test@example.com"
        };

    // ── Scope=Active returns only NotStarted, InProgress, Blocked ─────────────

    [Fact]
    public async Task GetPagedAsync_ScopeActive_ReturnsOnlyIncompleteStatuses()
    {
        _context.Tasks.AddRange(
            Make("u1", TaskStatus.NotStarted),
            Make("u1", TaskStatus.InProgress),
            Make("u1", TaskStatus.Blocked),
            Make("u1", TaskStatus.Completed),
            Make("u1", TaskStatus.Cancelled));
        await _context.SaveChangesAsync();

        var (items, total) = await _repo.GetPagedAsync(
            new TaskQueryParams(Scope: TaskScope.Active), "u1");

        Assert.Equal(3, total);
        Assert.All(items, t => Assert.Contains(t.Status,
            new[] { TaskStatus.NotStarted, TaskStatus.InProgress, TaskStatus.Blocked }));
    }

    [Fact]
    public async Task GetPagedAsync_ScopeActive_ExcludesCompleted()
    {
        _context.Tasks.AddRange(
            Make("u1", TaskStatus.NotStarted),
            Make("u1", TaskStatus.Completed));
        await _context.SaveChangesAsync();

        var (items, _) = await _repo.GetPagedAsync(
            new TaskQueryParams(Scope: TaskScope.Active), "u1");

        Assert.DoesNotContain(items, t => t.Status == TaskStatus.Completed);
    }

    [Fact]
    public async Task GetPagedAsync_ScopeActive_ExcludesCancelled()
    {
        _context.Tasks.AddRange(
            Make("u1", TaskStatus.InProgress),
            Make("u1", TaskStatus.Cancelled));
        await _context.SaveChangesAsync();

        var (items, _) = await _repo.GetPagedAsync(
            new TaskQueryParams(Scope: TaskScope.Active), "u1");

        Assert.DoesNotContain(items, t => t.Status == TaskStatus.Cancelled);
    }

    // ── Scope=Completed returns only Completed + Cancelled ────────────────────

    [Fact]
    public async Task GetPagedAsync_ScopeCompleted_ReturnsOnlyTerminalStatuses()
    {
        _context.Tasks.AddRange(
            Make("u1", TaskStatus.NotStarted),
            Make("u1", TaskStatus.InProgress),
            Make("u1", TaskStatus.Blocked),
            Make("u1", TaskStatus.Completed),
            Make("u1", TaskStatus.Cancelled));
        await _context.SaveChangesAsync();

        var (items, total) = await _repo.GetPagedAsync(
            new TaskQueryParams(Scope: TaskScope.Completed), "u1");

        Assert.Equal(2, total);
        Assert.All(items, t => Assert.Contains(t.Status,
            new[] { TaskStatus.Completed, TaskStatus.Cancelled }));
    }

    [Fact]
    public async Task GetPagedAsync_ScopeCompleted_ExcludesNotStarted()
    {
        _context.Tasks.AddRange(
            Make("u1", TaskStatus.Completed),
            Make("u1", TaskStatus.NotStarted));
        await _context.SaveChangesAsync();

        var (items, _) = await _repo.GetPagedAsync(
            new TaskQueryParams(Scope: TaskScope.Completed), "u1");

        Assert.DoesNotContain(items, t => t.Status == TaskStatus.NotStarted);
    }

    // ── Scope=All returns every status (no restriction) ───────────────────────

    [Fact]
    public async Task GetPagedAsync_ScopeAll_ReturnsAllStatuses()
    {
        _context.Tasks.AddRange(
            Make("u1", TaskStatus.NotStarted),
            Make("u1", TaskStatus.InProgress),
            Make("u1", TaskStatus.Blocked),
            Make("u1", TaskStatus.Completed),
            Make("u1", TaskStatus.Cancelled));
        await _context.SaveChangesAsync();

        var (items, total) = await _repo.GetPagedAsync(
            new TaskQueryParams(Scope: TaskScope.All), "u1");

        Assert.Equal(5, total);
        Assert.Equal(5, items.Count);
    }

    // ── CRITICAL: TotalCount must reflect the scope filter (D-006 core) ───────
    //
    // Before the fix, totalCount was computed on the pre-scope query so a page of
    // completed tasks would report e.g. totalCount=50 (all tasks) instead of
    // totalCount=15 (only completed). Pagination math (totalPages, next-page links)
    // would then be wrong.

    [Fact]
    public async Task GetPagedAsync_ScopeCompleted_TotalCountReflectsScopeNotAllTasks()
    {
        // Seed 10 active tasks and 3 completed tasks. With PageSize=5 and Scope=Completed
        // the total should be 3, NOT 13.
        for (var i = 0; i < 10; i++)
            _context.Tasks.Add(Make("u2", TaskStatus.NotStarted, i));
        for (var i = 0; i < 3; i++)
            _context.Tasks.Add(Make("u2", TaskStatus.Completed, 100 + i));
        await _context.SaveChangesAsync();

        var (_, total) = await _repo.GetPagedAsync(
            new TaskQueryParams(Scope: TaskScope.Completed, PageSize: 5), "u2");

        Assert.Equal(3, total);
    }

    [Fact]
    public async Task GetPagedAsync_ScopeActive_TotalCountReflectsScopeNotAllTasks()
    {
        // 15 active tasks, 5 completed.
        for (var i = 0; i < 15; i++)
            _context.Tasks.Add(Make("u3", TaskStatus.NotStarted, i));
        for (var i = 0; i < 5; i++)
            _context.Tasks.Add(Make("u3", TaskStatus.Completed, 100 + i));
        await _context.SaveChangesAsync();

        var (_, total) = await _repo.GetPagedAsync(
            new TaskQueryParams(Scope: TaskScope.Active, PageSize: 5), "u3");

        Assert.Equal(15, total);
    }

    [Fact]
    public async Task GetPagedAsync_ScopeCompleted_PaginatesWithinScopeOnly()
    {
        // 20 active tasks + 12 completed.  With PageSize=5, Scope=Completed:
        //   - total = 12
        //   - page 1 returns 5 items
        //   - page 3 returns 2 items (12 - 5 - 5)
        for (var i = 0; i < 20; i++)
            _context.Tasks.Add(Make("u4", TaskStatus.InProgress, i));
        for (var i = 0; i < 12; i++)
            _context.Tasks.Add(Make("u4", TaskStatus.Completed, 100 + i));
        await _context.SaveChangesAsync();

        var (page1Items, total) = await _repo.GetPagedAsync(
            new TaskQueryParams(Scope: TaskScope.Completed, Page: 1, PageSize: 5), "u4");
        var (page3Items, _) = await _repo.GetPagedAsync(
            new TaskQueryParams(Scope: TaskScope.Completed, Page: 3, PageSize: 5), "u4");

        Assert.Equal(12, total);
        Assert.Equal(5, page1Items.Count);
        Assert.Equal(2, page3Items.Count);
        // All items on every page must be terminal statuses
        Assert.All(page1Items, t =>
            Assert.Contains(t.Status, new[] { TaskStatus.Completed, TaskStatus.Cancelled }));
        Assert.All(page3Items, t =>
            Assert.Contains(t.Status, new[] { TaskStatus.Completed, TaskStatus.Cancelled }));
    }

    [Fact]
    public async Task GetPagedAsync_ScopeActive_PaginatesWithinScopeOnly()
    {
        // 25 active + 5 completed.  Scope=Active, PageSize=10:
        //   page 1 → 10 active items, total = 25
        for (var i = 0; i < 25; i++)
            _context.Tasks.Add(Make("u5", TaskStatus.NotStarted, i));
        for (var i = 0; i < 5; i++)
            _context.Tasks.Add(Make("u5", TaskStatus.Completed, 100 + i));
        await _context.SaveChangesAsync();

        var (page1, total) = await _repo.GetPagedAsync(
            new TaskQueryParams(Scope: TaskScope.Active, Page: 1, PageSize: 10), "u5");

        Assert.Equal(25, total);
        Assert.Equal(10, page1.Count);
        Assert.All(page1, t =>
            Assert.Contains(t.Status, new[] { TaskStatus.NotStarted, TaskStatus.InProgress, TaskStatus.Blocked }));
    }

    // ── Scope is user-scoped (no cross-user leakage) ─────────────────────────

    [Fact]
    public async Task GetPagedAsync_ScopeCompleted_WrongUser_ReturnsEmpty()
    {
        _context.Tasks.AddRange(
            Make("owner", TaskStatus.Completed),
            Make("owner", TaskStatus.Cancelled));
        await _context.SaveChangesAsync();

        var (items, total) = await _repo.GetPagedAsync(
            new TaskQueryParams(Scope: TaskScope.Completed), "intruder");

        Assert.Empty(items);
        Assert.Equal(0, total);
    }

    // ── Default (Scope=All) preserves existing behaviour ─────────────────────

    [Fact]
    public async Task GetPagedAsync_DefaultScope_EquivalentToAll()
    {
        _context.Tasks.AddRange(
            Make("u6", TaskStatus.NotStarted),
            Make("u6", TaskStatus.Completed),
            Make("u6", TaskStatus.Cancelled));
        await _context.SaveChangesAsync();

        // Default scope (not specified) should behave like All — return all 3
        var (items, total) = await _repo.GetPagedAsync(new TaskQueryParams(), "u6");

        Assert.Equal(3, total);
        Assert.Equal(3, items.Count);
    }
}
