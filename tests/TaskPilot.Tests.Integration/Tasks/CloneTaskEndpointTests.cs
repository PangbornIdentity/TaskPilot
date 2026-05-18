using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TaskPilot.Data;
using TaskPilot.Entities;
using TaskPilot.Tests.Integration.Helpers;

namespace TaskPilot.Tests.Integration.Tasks;

[Collection("Integration")]
public class CloneTaskEndpointTests : IClassFixture<TaskPilotWebAppFactory>
{
    private readonly TaskPilotWebAppFactory _factory;

    public CloneTaskEndpointTests(TaskPilotWebAppFactory factory)
    {
        _factory = factory;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static object ValidCreateTaskRequest(string title = "Source Task") => new
    {
        Title = title,
        Description = (string?)null,
        TaskTypeId = 1,
        Area = 0,
        Priority = 1,
        Status = 2, // InProgress
        TargetDateType = 1,
        TargetDate = (DateTime?)null,
        IsRecurring = false,
        RecurrencePattern = (int?)null,
        TagIds = (List<Guid>?)null
    };

    private static async Task<Guid> SeedTaskAsync(HttpClient client, string title = "Source Task", object? body = null)
    {
        var resp = await client.PostAsJsonAsync("/api/v1/tasks", body ?? ValidCreateTaskRequest(title));
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return Guid.Parse(json.GetProperty("data").GetProperty("id").GetString()!);
    }

    private static async Task<Guid> SeedTagAsync(HttpClient client, string name = "urgent")
    {
        var resp = await client.PostAsJsonAsync("/api/v1/tags", new { name, color = "#ff0000" });
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return Guid.Parse(json.GetProperty("data").GetProperty("id").GetString()!);
    }

    // ── I-CL-001: 201 Created ────────────────────────────────────────────────

    [Fact]
    public async Task CloneTask_ValidId_Returns201()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(client);

        var resp = await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    // ── I-CL-002: Standard envelope ──────────────────────────────────────────

    [Fact]
    public async Task CloneTask_ValidId_ResponseIsStandardEnvelope()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(client);

        var resp = await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { });
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(body.TryGetProperty("data", out _));
        Assert.True(body.TryGetProperty("meta", out var meta));
        var requestId = meta.GetProperty("requestId").GetString();
        Assert.False(string.IsNullOrEmpty(requestId));
        Assert.True(Guid.TryParse(requestId, out _));
    }

    // ── I-CL-003: Location header ─────────────────────────────────────────────

    [Fact]
    public async Task CloneTask_ValidId_LocationHeaderPointsToNewTask()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(client);

        var resp = await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { });
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var newId = body.GetProperty("data").GetProperty("id").GetString();

        Assert.NotNull(resp.Headers.Location);
        Assert.Contains(newId!, resp.Headers.Location!.ToString());
    }

    // ── I-CL-004: New task retrievable ────────────────────────────────────────

    [Fact]
    public async Task CloneTask_ValidId_NewTaskExistsInDb()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(client);

        var resp = await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { });
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var newId = body.GetProperty("data").GetProperty("id").GetString();

        var getResp = await client.GetAsync($"/api/v1/tasks/{newId}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
    }

    // ── I-CL-005: Default title ───────────────────────────────────────────────

    [Fact]
    public async Task CloneTask_DefaultTitle_HasCopySuffix()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(client, "My Task");

        var resp = await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { });
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("My Task (copy)", body.GetProperty("data").GetProperty("title").GetString());
    }

    // ── I-CL-006: Title override ──────────────────────────────────────────────

    [Fact]
    public async Task CloneTask_TitleOverride_UsesProvidedTitle()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(client);

        var resp = await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { title = "New Title" });
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("New Title", body.GetProperty("data").GetProperty("title").GetString());
    }

    // ── I-CL-007: Status is always NotStarted ────────────────────────────────

    [Fact]
    public async Task CloneTask_StatusIsAlwaysNotStarted()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(client); // seeded as InProgress (status=2)

        var resp = await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { });
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(0, body.GetProperty("data").GetProperty("status").GetInt32()); // NotStarted = 0
    }

    // ── I-CL-008: Completed source → NotStarted ───────────────────────────────

    [Fact]
    public async Task CloneTask_CompletedSource_StatusIsNotStarted()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(client);
        await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/complete", new { });

        var resp = await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { });
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(0, body.GetProperty("data").GetProperty("status").GetInt32()); // NotStarted = 0
    }

    // ── I-CL-009: Completed source → CompletedDate null ──────────────────────

    [Fact]
    public async Task CloneTask_CompletedSource_CompletedDateIsNull()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(client);
        await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/complete", new { });

        var resp = await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { });
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var completedDate = body.GetProperty("data").GetProperty("completedDate");

        Assert.Equal(JsonValueKind.Null, completedDate.ValueKind);
    }

    // ── I-CL-010: Completed source → ResultAnalysis null ─────────────────────

    [Fact]
    public async Task CloneTask_CompletedSource_ResultAnalysisIsNull()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(client);
        await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/complete", new { resultAnalysis = "Analysis text" });

        var resp = await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { });
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var ra = body.GetProperty("data").GetProperty("resultAnalysis");

        Assert.Equal(JsonValueKind.Null, ra.ValueKind);
    }

    // ── I-CL-011: Tags copied ─────────────────────────────────────────────────

    [Fact]
    public async Task CloneTask_TagsCopiedToClone()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var tag1 = await SeedTagAsync(client, "alpha");
        var tag2 = await SeedTagAsync(client, "beta");

        var taskId = await SeedTaskAsync(client, body: new
        {
            Title = "Tagged Task",
            TaskTypeId = 1,
            Area = 0,
            Priority = 1,
            Status = 0,
            TargetDateType = 1,
            IsRecurring = false,
            TagIds = new[] { tag1, tag2 }
        });

        var resp = await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { });
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var tags = body.GetProperty("data").GetProperty("tags");

        Assert.Equal(2, tags.GetArrayLength());
    }

    // ── I-CL-012: TargetDate copied when no override ─────────────────────────

    [Fact]
    public async Task CloneTask_TargetDateCopiedWhenNoOverride()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var targetDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var taskId = await SeedTaskAsync(client, body: new
        {
            Title = "Dated Task",
            TaskTypeId = 1,
            Area = 0,
            Priority = 1,
            Status = 0,
            TargetDateType = 0, // SpecificDay
            TargetDate = targetDate,
            IsRecurring = false
        });

        var resp = await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { });
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var cloneDate = body.GetProperty("data").GetProperty("targetDate").GetString();

        Assert.Contains("2026-06-01", cloneDate);
    }

    // ── I-CL-013: TargetDate override ────────────────────────────────────────

    [Fact]
    public async Task CloneTask_TargetDateOverride_AppliesOverride()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(client, body: new
        {
            Title = "Dated Task 2",
            TaskTypeId = 1,
            Area = 0,
            Priority = 1,
            Status = 0,
            TargetDateType = 0,
            TargetDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            IsRecurring = false
        });

        var resp = await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone",
            new { targetDate = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc) });
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var cloneDate = body.GetProperty("data").GetProperty("targetDate").GetString();

        Assert.Contains("2026-07-15", cloneDate);
    }

    // ── I-CL-014: clearTargetDate sets null ──────────────────────────────────

    [Fact]
    public async Task CloneTask_ClearTargetDate_SetsNull()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(client, body: new
        {
            Title = "Dated Task 3",
            TaskTypeId = 1,
            Area = 0,
            Priority = 1,
            Status = 0,
            TargetDateType = 0,
            TargetDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            IsRecurring = false
        });

        var resp = await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { clearTargetDate = true });
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var td = body.GetProperty("data").GetProperty("targetDate");

        Assert.Equal(JsonValueKind.Null, td.ValueKind);
    }

    // ── I-CL-015: No TargetDate → clone has none ─────────────────────────────

    [Fact]
    public async Task CloneTask_SourceHasNoTargetDate_CloneAlsoHasNone()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(client);

        var resp = await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { });
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var td = body.GetProperty("data").GetProperty("targetDate");

        Assert.Equal(JsonValueKind.Null, td.ValueKind);
    }

    // ── I-CL-016: Activity log — exactly one entry ────────────────────────────

    [Fact]
    public async Task CloneTask_ActivityLog_OneEntryOnClone()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(client);

        var cloneResp = await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { });
        var cloneBody = await cloneResp.Content.ReadFromJsonAsync<JsonElement>();
        var newId = cloneBody.GetProperty("data").GetProperty("id").GetString();

        var logResp = await client.GetAsync($"/api/v1/activity-logs?taskId={newId}");
        var logBody = await logResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, logBody.GetProperty("meta").GetProperty("totalCount").GetInt32());
    }

    // ── I-CL-017: Activity log — FieldChanged = "Created" ────────────────────

    [Fact]
    public async Task CloneTask_ActivityLog_FieldChangedIsCreated()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(client);

        var cloneResp = await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { });
        var cloneBody = await cloneResp.Content.ReadFromJsonAsync<JsonElement>();
        var newId = cloneBody.GetProperty("data").GetProperty("id").GetString();

        var logResp = await client.GetAsync($"/api/v1/activity-logs?taskId={newId}");
        var logBody = await logResp.Content.ReadFromJsonAsync<JsonElement>();
        var entry = logBody.GetProperty("data")[0];
        Assert.Equal("Created", entry.GetProperty("fieldChanged").GetString());
    }

    // ── I-CL-018: Activity log — NewValue contains sourceId ───────────────────

    [Fact]
    public async Task CloneTask_ActivityLog_NewValueContainsSourceId()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(client);

        var cloneResp = await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { });
        var cloneBody = await cloneResp.Content.ReadFromJsonAsync<JsonElement>();
        var newId = cloneBody.GetProperty("data").GetProperty("id").GetString();

        var logResp = await client.GetAsync($"/api/v1/activity-logs?taskId={newId}");
        var logBody = await logResp.Content.ReadFromJsonAsync<JsonElement>();
        var newValue = logBody.GetProperty("data")[0].GetProperty("newValue").GetString();

        Assert.Equal($"Cloned from {taskId:D}", newValue);
    }

    // ── I-CL-019: Source task activity log unchanged ──────────────────────────

    [Fact]
    public async Task CloneTask_SourceTask_ActivityLogUnchanged()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(client);

        // Get source log count before clone
        var beforeResp = await client.GetAsync($"/api/v1/activity-logs?taskId={taskId}");
        var beforeBody = await beforeResp.Content.ReadFromJsonAsync<JsonElement>();
        var before = beforeBody.GetProperty("meta").GetProperty("totalCount").GetInt32();

        await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { });

        var afterResp = await client.GetAsync($"/api/v1/activity-logs?taskId={taskId}");
        var afterBody = await afterResp.Content.ReadFromJsonAsync<JsonElement>();
        var after = afterBody.GetProperty("meta").GetProperty("totalCount").GetInt32();

        Assert.Equal(before, after);
    }

    // ── I-CL-020: Empty body (no Content-Length) ──────────────────────────────

    [Fact]
    public async Task CloneTask_EmptyBody_IsAccepted()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(client);

        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/tasks/{taskId}/clone");
        req.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        var resp = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    // ── I-CL-021: Recurring source → recurrence copied ───────────────────────

    [Fact]
    public async Task CloneTask_RecurringSource_RecurrenceCopied()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(client, body: new
        {
            Title = "Recurring Task",
            TaskTypeId = 1,
            Area = 0,
            Priority = 1,
            Status = 0,
            TargetDateType = 1,
            IsRecurring = true,
            RecurrencePattern = 1 // Weekly
        });

        var resp = await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { });
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");

        Assert.True(data.GetProperty("isRecurring").GetBoolean());
        Assert.Equal(1, data.GetProperty("recurrencePattern").GetInt32()); // Weekly = 1
    }

    // ── Auth tests ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CloneTask_Unauthenticated_Returns401()
    {
        var unauthClient = _factory.CreateClient();
        var resp = await unauthClient.PostAsJsonAsync($"/api/v1/tasks/{Guid.NewGuid()}/clone", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task CloneTask_CookieAuth_Returns201()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(client);
        var resp = await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task CloneTask_ApiKeyAuth_Returns201()
    {
        var (cookieClient, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(cookieClient);

        // Generate an API key
        var keyResp = await cookieClient.PostAsJsonAsync("/api/v1/apikeys", new { Name = "TestKey-Clone" });
        keyResp.EnsureSuccessStatusCode();
        var keyBody = await keyResp.Content.ReadFromJsonAsync<JsonElement>();
        var fullKey = keyBody.GetProperty("data").GetProperty("plainTextKey").GetString()!;

        var apiKeyClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
        apiKeyClient.DefaultRequestHeaders.Add("X-Api-Key", fullKey);

        var resp = await apiKeyClient.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task CloneTask_ApiKeyAuth_LastModifiedByIsApiKeyName()
    {
        var (cookieClient, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(cookieClient);

        var keyResp = await cookieClient.PostAsJsonAsync("/api/v1/apikeys", new { Name = "Claude-Work" });
        var keyBody = await keyResp.Content.ReadFromJsonAsync<JsonElement>();
        var fullKey = keyBody.GetProperty("data").GetProperty("plainTextKey").GetString()!;

        var apiKeyClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false, HandleCookies = false
        });
        apiKeyClient.DefaultRequestHeaders.Add("X-Api-Key", fullKey);

        var resp = await apiKeyClient.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { });
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("api:Claude-Work", body.GetProperty("data").GetProperty("lastModifiedBy").GetString());
    }

    [Fact]
    public async Task CloneTask_CookieAuth_LastModifiedByIsUsername()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(client);

        var resp = await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { });
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var lmb = body.GetProperty("data").GetProperty("lastModifiedBy").GetString();

        Assert.StartsWith("user:", lmb);
    }

    // ── Error tests ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CloneTask_MissingId_Returns404()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var resp = await client.PostAsJsonAsync($"/api/v1/tasks/{Guid.NewGuid()}/clone", new { });

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("NOT_FOUND", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task CloneTask_SoftDeletedSource_Returns404()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(client);
        await client.DeleteAsync($"/api/v1/tasks/{taskId}");

        var resp = await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { });

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task CloneTask_SoftDeletedSource_SameBodyAsMissing()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(client);
        await client.DeleteAsync($"/api/v1/tasks/{taskId}");

        var deletedResp = await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { });
        var deletedBody = await deletedResp.Content.ReadFromJsonAsync<JsonElement>();

        var missingResp = await client.PostAsJsonAsync($"/api/v1/tasks/{Guid.NewGuid()}/clone", new { });
        var missingBody = await missingResp.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            deletedBody.GetProperty("error").GetProperty("code").GetString(),
            missingBody.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task CloneTask_AnotherUsersTask_Returns404()
    {
        var (clientA, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var (clientB, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(clientA);

        var resp = await clientB.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { });

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task CloneTask_TitleExceeds200Chars_Returns400()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(client);
        var longTitle = new string('x', 201);

        var resp = await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { title = longTitle });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VALIDATION_ERROR", body.GetProperty("error").GetProperty("code").GetString());
    }

    // ── Audit middleware ──────────────────────────────────────────────────────

    [Fact]
    public async Task CloneTask_ApiKeyAuth_AuditLogEntryCreated()
    {
        var (cookieClient, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(cookieClient);

        var keyResp = await cookieClient.PostAsJsonAsync("/api/v1/apikeys", new { Name = "AuditKey-Clone" });
        var keyBody = await keyResp.Content.ReadFromJsonAsync<JsonElement>();
        var fullKey = keyBody.GetProperty("data").GetProperty("plainTextKey").GetString()!;

        var apiKeyClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false, HandleCookies = false
        });
        apiKeyClient.DefaultRequestHeaders.Add("X-Api-Key", fullKey);

        await apiKeyClient.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { });

        var auditResp = await cookieClient.GetAsync("/api/v1/audit");
        var auditBody = await auditResp.Content.ReadFromJsonAsync<JsonElement>();
        var entries = auditBody.GetProperty("data");

        var found = false;
        foreach (var entry in entries.EnumerateArray())
        {
            var method = entry.GetProperty("httpMethod").GetString();
            var endpoint = entry.GetProperty("endpoint").GetString() ?? "";
            if (method == "POST" && endpoint.Contains("clone", StringComparison.OrdinalIgnoreCase))
            {
                found = true;
                break;
            }
        }
        Assert.True(found, "Expected an audit log entry for POST /clone");
    }

    [Fact]
    public async Task CloneTask_CookieAuth_NoAuditLogEntry()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var taskId = await SeedTaskAsync(client);

        // Get count before
        var beforeResp = await client.GetAsync("/api/v1/audit");
        var beforeBody = await beforeResp.Content.ReadFromJsonAsync<JsonElement>();
        var before = beforeBody.GetProperty("meta").GetProperty("totalCount").GetInt32();

        await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { });

        var afterResp = await client.GetAsync("/api/v1/audit");
        var afterBody = await afterResp.Content.ReadFromJsonAsync<JsonElement>();
        var after = afterBody.GetProperty("meta").GetProperty("totalCount").GetInt32();

        Assert.Equal(before, after);
    }

    // ── I-CL-034: Atomicity ───────────────────────────────────────────────────

    [Fact]
    public async Task CloneTask_SaveFailure_NoPartialRowsInDb()
    {
        // Factory whose DbContext throws on SaveChangesAsync only when a static flag is set.
        // Auth + seed run with the flag off; clone runs with it on, so we can assert that
        // a failed clone leaves zero new rows (i.e. CloneTaskAsync's single SaveChangesAsync
        // is wrapped in EF Core's default transaction).
        ThrowingDbContext.ShouldThrow = false;

        using var throwingFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var existing = services.SingleOrDefault(d => d.ServiceType == typeof(ApplicationDbContext));
                if (existing != null) services.Remove(existing);

                services.AddScoped<ApplicationDbContext>(sp =>
                {
                    var options = sp.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
                    return new ThrowingDbContext(options);
                });
            });
        });

        try
        {
            var (client, userId) = await AuthHelper.CreateAuthenticatedClientAsync(throwingFactory);
            var taskId = await SeedTaskAsync(client);

            int tasksBefore, logsBefore;
            using (var scope = throwingFactory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                tasksBefore = await db.Tasks.IgnoreQueryFilters().CountAsync();
                logsBefore = await db.TaskActivityLogs.CountAsync();
            }

            ThrowingDbContext.ShouldThrow = true;
            var resp = await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/clone", new { });
            Assert.True((int)resp.StatusCode >= 500, $"Expected 5xx, got {(int)resp.StatusCode}");

            ThrowingDbContext.ShouldThrow = false;
            using (var scope = throwingFactory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var tasksAfter = await db.Tasks.IgnoreQueryFilters().CountAsync();
                var logsAfter = await db.TaskActivityLogs.CountAsync();
                Assert.Equal(tasksBefore, tasksAfter);
                Assert.Equal(logsBefore, logsAfter);
            }
        }
        finally
        {
            ThrowingDbContext.ShouldThrow = false;
        }
    }

    /// <summary>
    /// Test DbContext subclass whose SaveChangesAsync throws only when
    /// <see cref="ShouldThrow"/> is true. The flag is process-wide static — fine for
    /// xUnit's per-collection serial execution, but do not parallelise this test class.
    /// </summary>
    private sealed class ThrowingDbContext(DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        public static bool ShouldThrow { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => ShouldThrow
                ? throw new InvalidOperationException("Simulated save failure for atomicity test.")
                : base.SaveChangesAsync(cancellationToken);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskItem>(b =>
            {
                b.HasKey(t => t.Id);
                b.Property(t => t.Title).IsRequired().HasMaxLength(200);
                b.Property(t => t.UserId).IsRequired();
                b.Property(t => t.LastModifiedBy).IsRequired();
                b.Property(t => t.Area).HasConversion<int>();
                b.Property(t => t.Priority).HasConversion<int>();
                b.Property(t => t.Status).HasConversion<int>();
                b.Property(t => t.TargetDateType).HasConversion<int>();
                b.Property(t => t.RecurrencePattern).HasConversion<int?>();
                b.HasQueryFilter(t => !t.IsDeleted);
                b.HasMany(t => t.TaskTags).WithOne(tt => tt.Task).HasForeignKey(tt => tt.TaskId).OnDelete(DeleteBehavior.Cascade);
                b.HasMany(t => t.ActivityLogs).WithOne(a => a.Task).HasForeignKey(a => a.TaskId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<Tag>(b =>
            {
                b.HasKey(t => t.Id);
                b.Property(t => t.Name).IsRequired().HasMaxLength(50);
                b.Property(t => t.Color).IsRequired().HasMaxLength(7);
                b.Property(t => t.UserId).IsRequired();
                b.HasIndex(t => new { t.UserId, t.Name }).IsUnique();
            });
            modelBuilder.Entity<TaskTag>(b =>
            {
                b.HasKey(tt => new { tt.TaskId, tt.TagId });
                b.HasOne(tt => tt.Tag).WithMany(t => t.TaskTags).HasForeignKey(tt => tt.TagId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<TaskType>(b =>
            {
                b.HasKey(t => t.Id);
                b.Property(t => t.Id).ValueGeneratedOnAdd();
                b.Property(t => t.Name).IsRequired().HasMaxLength(50);
                b.HasData(
                    new TaskType { Id = 1, Name = "Task", SortOrder = 1, IsActive = true },
                    new TaskType { Id = 2, Name = "Goal", SortOrder = 2, IsActive = true },
                    new TaskType { Id = 3, Name = "Habit", SortOrder = 3, IsActive = true },
                    new TaskType { Id = 4, Name = "Meeting", SortOrder = 4, IsActive = true },
                    new TaskType { Id = 5, Name = "Note", SortOrder = 5, IsActive = true },
                    new TaskType { Id = 6, Name = "Event", SortOrder = 6, IsActive = true }
                );
            });
            modelBuilder.Entity<TaskActivityLog>(b =>
            {
                b.HasKey(a => a.Id);
                b.Property(a => a.ChangedBy).IsRequired();
            });
            modelBuilder.Entity<TaskPilot.Entities.ApiKey>(b =>
            {
                b.HasKey(k => k.Id);
                b.Property(k => k.Name).IsRequired().HasMaxLength(100);
                b.Property(k => k.KeyHash).IsRequired();
                b.Property(k => k.KeyPrefix).IsRequired().HasMaxLength(8);
                b.Property(k => k.UserId).IsRequired();
                b.HasIndex(k => new { k.UserId, k.Name }).IsUnique();
            });
            modelBuilder.Entity<ApiAuditLog>(b => b.HasKey(a => a.Id));

            // Identity entities
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUser>(b =>
            {
                b.HasKey(u => u.Id);
                b.Property(u => u.UserName).HasMaxLength(256);
                b.Property(u => u.NormalizedUserName).HasMaxLength(256);
                b.Property(u => u.Email).HasMaxLength(256);
                b.Property(u => u.NormalizedEmail).HasMaxLength(256);
                b.HasIndex(u => u.NormalizedUserName).IsUnique().HasFilter(null);
                b.HasIndex(u => u.NormalizedEmail);
                b.HasMany<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().WithOne().HasForeignKey(uc => uc.UserId).IsRequired();
                b.HasMany<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().WithOne().HasForeignKey(ul => ul.UserId).IsRequired();
                b.HasMany<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().WithOne().HasForeignKey(ut => ut.UserId).IsRequired();
                b.HasMany<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().WithOne().HasForeignKey(ur => ur.UserId).IsRequired();
                b.ToTable("AspNetUsers");
            });
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>(b => { b.HasKey(uc => uc.Id); b.ToTable("AspNetUserClaims"); });
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>(b => { b.HasKey(ul => new { ul.LoginProvider, ul.ProviderKey }); b.ToTable("AspNetUserLogins"); });
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>(b => { b.HasKey(ut => new { ut.UserId, ut.LoginProvider, ut.Name }); b.ToTable("AspNetUserTokens"); });
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>(b => { b.HasKey(r => r.Id); b.Property(r => r.Name).HasMaxLength(256); b.Property(r => r.NormalizedName).HasMaxLength(256); b.HasIndex(r => r.NormalizedName).IsUnique().HasFilter(null); b.HasMany<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().WithOne().HasForeignKey(ur => ur.RoleId).IsRequired(); b.HasMany<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().WithOne().HasForeignKey(rc => rc.RoleId).IsRequired(); b.ToTable("AspNetRoles"); });
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>(b => { b.HasKey(rc => rc.Id); b.ToTable("AspNetRoleClaims"); });
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>(b => { b.HasKey(ur => new { ur.UserId, ur.RoleId }); b.ToTable("AspNetUserRoles"); });

            var identityAssembly = typeof(Microsoft.AspNetCore.Identity.IdentityUser).Assembly;
            var efAssembly = typeof(Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext).Assembly;
            foreach (var asm in new[] { identityAssembly, efAssembly })
            {
                foreach (var type in asm.GetExportedTypes()
                    .Where(t => t.Name.Contains("Passkey", StringComparison.OrdinalIgnoreCase) && t.IsClass && !t.IsAbstract))
                {
                    try { modelBuilder.Ignore(type); } catch { }
                    if (type.IsGenericTypeDefinition)
                        try { modelBuilder.Ignore(type.MakeGenericType(typeof(string))); } catch { }
                }
            }
        }
    }
}
