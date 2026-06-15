using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TaskPilot.Tests.Integration.Helpers;

namespace TaskPilot.Tests.Integration.Tasks;

/// <summary>
/// Integration tests proving defect fix D-006 at the HTTP layer:
///   GET /api/v1/tasks?scope=Active|Completed|All (or the /tasks Razor Page
///   with ?show=active|completed|all) returns the server-side filtered count
///   in meta.totalCount, not the total across all tasks.
///
/// Tests FAIL if scope filtering is moved to post-query client-side code
/// or if totalCount is computed before the scope predicate is applied.
///
/// REQ: FR-TASKS-025 (active/completed/all scope).
/// </summary>
[Collection("Integration")]
public class TaskScopeApiTests : IClassFixture<TaskPilotWebAppFactory>
{
    private readonly TaskPilotWebAppFactory _factory;

    public TaskScopeApiTests(TaskPilotWebAppFactory factory)
    {
        _factory = factory;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static object MakeTask(string title, int status) => new
    {
        Title = title,
        Description = (string?)null,
        TaskTypeId = 1,
        Area = 0,
        Priority = 1,
        Status = status,
        TargetDateType = 1,
        TargetDate = (DateTime?)null,
        IsRecurring = false,
        RecurrencePattern = (int?)null,
        TagIds = (List<Guid>?)null
    };

    /// <summary>
    /// Seeds a fixed mix for one user and returns the cookie client.
    /// Seeds: 3 NotStarted + 1 InProgress + 1 Blocked = 5 Active
    ///         2 Completed + 1 Cancelled                = 3 Terminal
    ///         total = 8
    /// </summary>
    private async Task<HttpClient> SeedMixAsync()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        // Status enum: NotStarted=0, InProgress=1, Blocked=2, Completed=3, Cancelled=4
        var suffix = Guid.NewGuid().ToString("N")[..8];
        (await client.PostAsJsonAsync("/api/v1/tasks", MakeTask($"ns1-{suffix}", 0))).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/v1/tasks", MakeTask($"ns2-{suffix}", 0))).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/v1/tasks", MakeTask($"ns3-{suffix}", 0))).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/v1/tasks", MakeTask($"ip1-{suffix}", 1))).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/v1/tasks", MakeTask($"blk1-{suffix}", 2))).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/v1/tasks", MakeTask($"done1-{suffix}", 3))).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/v1/tasks", MakeTask($"done2-{suffix}", 3))).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/v1/tasks", MakeTask($"can1-{suffix}", 4))).EnsureSuccessStatusCode();

        return client;
    }

    // ── Scope=Active: items and totalCount restricted to active tasks ─────────

    // <REQ-ID: FR-TASKS-025>
    [Fact]
    public async Task GetTasks_ScopeActive_ReturnsOnlyActiveStatuses()
    {
        var client = await SeedMixAsync();

        var response = await client.GetAsync("/api/v1/tasks?scope=1&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data").EnumerateArray().ToList();

        Assert.True(data.Count > 0, "Active scope should return at least one task.");
        Assert.All(data, t =>
        {
            var status = t.GetProperty("status").GetInt32();
            Assert.True(status >= 0 && status <= 2,
                $"Status {status} is not active (expected 0=NotStarted, 1=InProgress, 2=Blocked).");
        });
    }

    // <REQ-ID: FR-TASKS-025>
    [Fact]
    public async Task GetTasks_ScopeActive_TotalCountExcludesTerminalTasks()
    {
        var client = await SeedMixAsync();

        // pageSize=2 so we definitely don't get everything in one page.
        // totalCount must still equal the count of ACTIVE tasks only, not all 8.
        var response = await client.GetAsync("/api/v1/tasks?scope=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var meta = body.GetProperty("meta");
        var totalCount = meta.GetProperty("totalCount").GetInt32();

        // We seeded 5 active tasks (3 NS + 1 IP + 1 Blocked).
        Assert.Equal(5, totalCount);
    }

    // ── Scope=Completed: items and totalCount restricted to terminal tasks ─────

    // <REQ-ID: FR-TASKS-025>
    [Fact]
    public async Task GetTasks_ScopeCompleted_ReturnsOnlyTerminalStatuses()
    {
        var client = await SeedMixAsync();

        var response = await client.GetAsync("/api/v1/tasks?scope=2&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data").EnumerateArray().ToList();

        Assert.True(data.Count > 0, "Completed scope should return at least one task.");
        Assert.All(data, t =>
        {
            var status = t.GetProperty("status").GetInt32();
            // Completed=3, Cancelled=4
            Assert.True(status == 3 || status == 4,
                $"Status {status} should not appear in Completed scope.");
        });
    }

    // <REQ-ID: FR-TASKS-025>
    [Fact]
    public async Task GetTasks_ScopeCompleted_TotalCountExcludesActiveTasks()
    {
        var client = await SeedMixAsync();

        var response = await client.GetAsync("/api/v1/tasks?scope=2&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var meta = body.GetProperty("meta");
        var totalCount = meta.GetProperty("totalCount").GetInt32();

        // We seeded 3 terminal tasks (2 Completed + 1 Cancelled).
        Assert.Equal(3, totalCount);
    }

    // ── Scope=All: no status restriction ─────────────────────────────────────

    // <REQ-ID: FR-TASKS-025>
    [Fact]
    public async Task GetTasks_ScopeAll_ReturnsMixOfStatuses()
    {
        var client = await SeedMixAsync();

        var response = await client.GetAsync("/api/v1/tasks?scope=0&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data").EnumerateArray().ToList();
        var meta = body.GetProperty("meta");
        var totalCount = meta.GetProperty("totalCount").GetInt32();

        // Must include both active and terminal tasks.
        Assert.Equal(8, totalCount);
        Assert.Equal(8, data.Count);
    }

    // ── CRITICAL: totalCount reflects scope, not raw row count ───────────────
    //
    // This is the D-006 regression test. Before the fix:
    //   GET ?scope=2&pageSize=1  →  data has 1 item,  totalCount=8 (BUG)
    // After the fix:
    //   GET ?scope=2&pageSize=1  →  data has 1 item,  totalCount=3 (CORRECT)

    // <REQ-ID: FR-TASKS-025>
    [Fact]
    public async Task GetTasks_ScopeCompleted_WithSmallPage_TotalCountIsNotCapByPage()
    {
        var client = await SeedMixAsync();

        // pageSize=1 forces pagination; totalCount must still be 3 (all completed),
        // not 1 (just the current page) and not 8 (all tasks).
        var response = await client.GetAsync("/api/v1/tasks?scope=2&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(1, body.GetProperty("data").GetArrayLength());
        Assert.Equal(3, body.GetProperty("meta").GetProperty("totalCount").GetInt32());
    }

    // <REQ-ID: FR-TASKS-025>
    [Fact]
    public async Task GetTasks_ScopeActive_WithSmallPage_TotalCountIsNotCappedByPage()
    {
        var client = await SeedMixAsync();

        var response = await client.GetAsync("/api/v1/tasks?scope=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        // pageSize=2 → only 2 items in data, but totalCount must be 5 (all active)
        Assert.Equal(2, body.GetProperty("data").GetArrayLength());
        Assert.Equal(5, body.GetProperty("meta").GetProperty("totalCount").GetInt32());
    }

    // ── Scope does not expose other users' tasks ──────────────────────────────

    // <REQ-ID: FR-TASKS-025>
    [Fact]
    public async Task GetTasks_ScopeCompleted_DoesNotLeakOtherUsersCompletedTasks()
    {
        // User A seeds completed tasks, User B checks scope=Completed
        var (clientA, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        (await clientA.PostAsJsonAsync("/api/v1/tasks", MakeTask("UserADone", 3))).EnsureSuccessStatusCode();

        var (clientB, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var response = await clientB.GetAsync("/api/v1/tasks?scope=2&pageSize=50");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data").EnumerateArray().ToList();

        Assert.DoesNotContain(data, t =>
            t.GetProperty("title").GetString() == "UserADone");
    }

    // ── Scope meta has correct totalPages ────────────────────────────────────

    // <REQ-ID: FR-TASKS-025>
    [Fact]
    public async Task GetTasks_ScopeCompleted_MetaHasCorrectTotalPages()
    {
        var client = await SeedMixAsync();

        // 3 terminal tasks, pageSize=2 → 2 pages
        var response = await client.GetAsync("/api/v1/tasks?scope=2&pageSize=2");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var meta = body.GetProperty("meta");

        Assert.Equal(3, meta.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, meta.GetProperty("totalPages").GetInt32());
        Assert.Equal(1, meta.GetProperty("page").GetInt32());
        Assert.Equal(2, meta.GetProperty("pageSize").GetInt32());
    }
}
