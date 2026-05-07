using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TaskPilot.Tests.Integration.Helpers;

namespace TaskPilot.Tests.Integration.Tasks;

/// <summary>
/// Integration tests for the ?show= query parameter on GET /tasks (Razor Page)
/// added in v1.12.
///
/// The /tasks Razor Page uses a segmented control (Active / Completed / All) driven
/// by ?show=active|completed|all (default: active). The page model translates this
/// into IncludeOnlyIncomplete=true (active) or a post-query status filter (completed)
/// or no status filter (all).
///
/// These tests seed tasks in all five statuses, hit the rendered Razor page as an
/// authenticated cookie client, and verify the HTML body contains only the expected
/// task titles.
///
/// Note: The /api/v1/tasks endpoint does not have a show param; it has
/// includeOnlyIncomplete + overdueOnly. The show param lives exclusively on the
/// page layer. The existing API-level filter tests (GetTasks_WithIncludeOnlyIncomplete_*)
/// cover the repository filter; these tests cover the page-model translation.
/// </summary>
[Collection("Integration")]
public class TasksShowParamTests : IClassFixture<TaskPilotWebAppFactory>
{
    private readonly TaskPilotWebAppFactory _factory;

    public TasksShowParamTests(TaskPilotWebAppFactory factory)
    {
        _factory = factory;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static object MakeApiTask(string title, int status = 0, int priority = 1, DateTime? targetDate = null) => new
    {
        Title = title,
        Description = (string?)null,
        TaskTypeId = 1,
        Area = 0,       // Personal
        Priority = priority,
        Status = status,
        TargetDateType = 1, // ThisWeek
        TargetDate = targetDate,
        IsRecurring = false,
        RecurrencePattern = (int?)null,
        TagIds = (List<Guid>?)null
    };

    /// <summary>
    /// Seeds one task per status via the REST API, then returns an authenticated
    /// page client (cookie auth) plus each task title for assertions.
    /// Status enum: NotStarted=0, InProgress=1, Blocked=2, Completed=3, Cancelled=4
    /// </summary>
    private async Task<(HttpClient apiClient, HttpClient pageClient,
                        string nsTitle, string ipTitle, string blkTitle,
                        string doneTitle, string canTitle)>
        SeedAllStatusesAsync()
    {
        var (apiClient, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var ns   = $"shw-ns-{Guid.NewGuid():N}"[..20];
        var ip   = $"shw-ip-{Guid.NewGuid():N}"[..20];
        var blk  = $"shw-blk-{Guid.NewGuid():N}"[..20];
        var done = $"shw-done-{Guid.NewGuid():N}"[..20];
        var can  = $"shw-can-{Guid.NewGuid():N}"[..20];

        (await apiClient.PostAsJsonAsync("/api/v1/tasks", MakeApiTask(ns,   status: 0))).EnsureSuccessStatusCode();
        (await apiClient.PostAsJsonAsync("/api/v1/tasks", MakeApiTask(ip,   status: 1))).EnsureSuccessStatusCode();
        (await apiClient.PostAsJsonAsync("/api/v1/tasks", MakeApiTask(blk,  status: 2))).EnsureSuccessStatusCode();
        (await apiClient.PostAsJsonAsync("/api/v1/tasks", MakeApiTask(done, status: 3))).EnsureSuccessStatusCode();
        (await apiClient.PostAsJsonAsync("/api/v1/tasks", MakeApiTask(can,  status: 4))).EnsureSuccessStatusCode();

        // The REST API client already has an auth cookie; reuse it for page requests
        return (apiClient, apiClient, ns, ip, blk, done, can);
    }

    // ── I-SHW-001: default (no params) returns only active statuses ───────────

    [Fact]
    public async Task GetTasksPage_NoParams_DefaultsToActiveStatusesOnly()
    {
        var (_, client, ns, ip, blk, done, can) = await SeedAllStatusesAsync();

        var response = await client.GetAsync("/tasks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();

        // Active tasks must appear in the page
        Assert.Contains(ns,  html);
        Assert.Contains(ip,  html);
        Assert.Contains(blk, html);

        // Terminal tasks must NOT appear
        Assert.DoesNotContain(done, html);
        Assert.DoesNotContain(can,  html);
    }

    // ── I-SHW-002: show=completed returns Completed + Cancelled only ──────────

    [Fact]
    public async Task GetTasksPage_ShowCompleted_ReturnsOnlyTerminalStatuses()
    {
        var (_, client, ns, ip, blk, done, can) = await SeedAllStatusesAsync();

        var response = await client.GetAsync("/tasks?show=completed");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();

        // Terminal tasks must appear
        Assert.Contains(done, html);
        Assert.Contains(can,  html);

        // Active tasks must NOT appear
        Assert.DoesNotContain(ns,  html);
        Assert.DoesNotContain(ip,  html);
        Assert.DoesNotContain(blk, html);
    }

    // ── I-SHW-003: show=all returns every status ──────────────────────────────

    [Fact]
    public async Task GetTasksPage_ShowAll_ReturnsAllFiveStatuses()
    {
        var (_, client, ns, ip, blk, done, can) = await SeedAllStatusesAsync();

        var response = await client.GetAsync("/tasks?show=all");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();

        // All five seeded tasks must appear
        Assert.Contains(ns,   html);
        Assert.Contains(ip,   html);
        Assert.Contains(blk,  html);
        Assert.Contains(done, html);
        Assert.Contains(can,  html);
    }

    // ── I-SHW-004: show=active + overdue=true intersects correctly ────────────

    [Fact]
    public async Task GetTasksPage_ShowActiveAndOverdue_ReturnsOnlyOverdueActiveTasks()
    {
        var (apiClient, client, _, _, _, _, _) = await SeedAllStatusesAsync();

        var yesterday = DateTime.UtcNow.AddDays(-1);
        var tomorrow  = DateTime.UtcNow.AddDays(1);

        var overdueActive    = $"ov-act-{Guid.NewGuid():N}"[..20];
        var overdueCompleted = $"ov-done-{Guid.NewGuid():N}"[..20];
        var futureActive     = $"fut-act-{Guid.NewGuid():N}"[..20];
        var noDateActive     = $"nd-act-{Guid.NewGuid():N}"[..20];

        (await apiClient.PostAsJsonAsync("/api/v1/tasks", MakeApiTask(overdueActive,    status: 0, targetDate: yesterday))).EnsureSuccessStatusCode();
        (await apiClient.PostAsJsonAsync("/api/v1/tasks", MakeApiTask(overdueCompleted, status: 3, targetDate: yesterday))).EnsureSuccessStatusCode();
        (await apiClient.PostAsJsonAsync("/api/v1/tasks", MakeApiTask(futureActive,     status: 0, targetDate: tomorrow))).EnsureSuccessStatusCode();
        (await apiClient.PostAsJsonAsync("/api/v1/tasks", MakeApiTask(noDateActive,     status: 0))).EnsureSuccessStatusCode();

        var response = await client.GetAsync("/tasks?show=active&overdue=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains(overdueActive, html);
        Assert.DoesNotContain(overdueCompleted, html);
        Assert.DoesNotContain(futureActive,     html);
        Assert.DoesNotContain(noDateActive,     html);
    }

    // ── I-SHW-005: show=active + priority filter intersects correctly ─────────

    [Fact]
    public async Task GetTasksPage_ShowActiveAndPriority_ReturnsOnlyMatchingPriorityActiveTasks()
    {
        var (apiClient, client, _, _, _, _, _) = await SeedAllStatusesAsync();

        // Priority enum: Low=0, Medium=1, High=2, Critical=3
        var criticalActive    = $"crit-act-{Guid.NewGuid():N}"[..20];
        var criticalCompleted = $"crit-done-{Guid.NewGuid():N}"[..20];
        var mediumActive      = $"med-act-{Guid.NewGuid():N}"[..20];

        (await apiClient.PostAsJsonAsync("/api/v1/tasks", MakeApiTask(criticalActive,    status: 0, priority: 3))).EnsureSuccessStatusCode();
        (await apiClient.PostAsJsonAsync("/api/v1/tasks", MakeApiTask(criticalCompleted, status: 3, priority: 3))).EnsureSuccessStatusCode();
        (await apiClient.PostAsJsonAsync("/api/v1/tasks", MakeApiTask(mediumActive,      status: 0, priority: 1))).EnsureSuccessStatusCode();

        // Filter by priority=3 (Critical) with show=active
        var response = await client.GetAsync("/tasks?show=active&priority=3");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains(criticalActive, html);
        Assert.DoesNotContain(criticalCompleted, html); // active filter excludes Completed
        Assert.DoesNotContain(mediumActive, html);      // priority filter excludes Medium
    }

    // ── I-SHW-006: unknown show value falls back to active ────────────────────

    [Fact]
    public async Task GetTasksPage_UnknownShowParam_TreatedAsActive()
    {
        var (_, client, ns, ip, blk, done, can) = await SeedAllStatusesAsync();

        // "bogus" is not a valid show value; page model defaults to "active"
        var response = await client.GetAsync("/tasks?show=bogus");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();

        // Should behave like show=active: no terminal statuses
        Assert.DoesNotContain(done, html);
        Assert.DoesNotContain(can,  html);

        // Active tasks must appear
        Assert.Contains(ns, html);
    }
}
