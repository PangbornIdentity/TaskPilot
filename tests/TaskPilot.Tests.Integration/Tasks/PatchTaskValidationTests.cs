using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TaskPilot.Tests.Integration.Helpers;

namespace TaskPilot.Tests.Integration.Tasks;

/// <summary>
/// Integration tests proving defect fix D-001:
///   PATCH /api/v1/tasks/{id} now runs the new PatchTaskRequestValidator
///   before calling the service layer.
///
/// Every test FAILS if the PatchTaskRequestValidator or the controller's
/// validation block is reverted.
///
/// REQ: FR-TASKS-008 (partial update), FR-TASKS-035 (validation).
/// </summary>
[Collection("Integration")]
public class PatchTaskValidationTests : IClassFixture<TaskPilotWebAppFactory>
{
    private readonly TaskPilotWebAppFactory _factory;

    public PatchTaskValidationTests(TaskPilotWebAppFactory factory)
    {
        _factory = factory;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static object DefaultCreateBody(string title = "Test Task") => new
    {
        Title = title,
        Description = (string?)null,
        TaskTypeId = 1,
        Area = 0,
        Priority = 1,
        Status = 0,
        TargetDateType = 1,
        TargetDate = (DateTime?)null,
        IsRecurring = false,
        RecurrencePattern = (int?)null,
        TagIds = (List<Guid>?)null
    };

    private async Task<(HttpClient Client, string TaskId)> CreateTaskAsync(string title = "Patch Target")
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var resp = await client.PostAsJsonAsync("/api/v1/tasks", DefaultCreateBody(title));
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("data").GetProperty("id").GetString()!;
        return (client, id);
    }

    private static void AssertValidationErrorEnvelope(JsonElement body)
    {
        Assert.True(body.TryGetProperty("error", out var error),
            "Response must have an 'error' property.");
        Assert.True(error.TryGetProperty("code", out _),
            "Error object must have a 'code' property.");
        Assert.True(error.TryGetProperty("message", out _),
            "Error object must have a 'message' property.");
        Assert.True(error.TryGetProperty("details", out var details),
            "Validation errors must include 'details'.");
        Assert.True(details.GetArrayLength() > 0,
            "At least one field-level error must be present.");
    }

    // ── D-001 / FR-TASKS-035: empty Title → 400 ──────────────────────────────

    // <REQ-ID: FR-TASKS-035>
    [Fact]
    public async Task PatchTask_EmptyTitle_Returns400WithValidationErrorEnvelope()
    {
        var (client, taskId) = await CreateTaskAsync();

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/tasks/{taskId}",
            new { Title = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        AssertValidationErrorEnvelope(body);

        // The 'details' array must name the 'Title' field.
        var details = body.GetProperty("error").GetProperty("details");
        Assert.Contains(details.EnumerateArray(),
            d => d.GetProperty("field").GetString()!
                  .Equals("Title", StringComparison.OrdinalIgnoreCase));
    }

    // <REQ-ID: FR-TASKS-035>
    [Fact]
    public async Task PatchTask_TitleWhitespaceOnly_Returns400()
    {
        var (client, taskId) = await CreateTaskAsync();

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/tasks/{taskId}",
            new { Title = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // <REQ-ID: FR-TASKS-035>
    [Fact]
    public async Task PatchTask_Title201Chars_Returns400WithValidationErrorEnvelope()
    {
        var (client, taskId) = await CreateTaskAsync();

        var tooLong = new string('X', 201);
        var response = await client.PatchAsJsonAsync(
            $"/api/v1/tasks/{taskId}",
            new { Title = tooLong });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        AssertValidationErrorEnvelope(body);
        var details = body.GetProperty("error").GetProperty("details");
        Assert.Contains(details.EnumerateArray(),
            d => d.GetProperty("field").GetString()!
                  .Equals("Title", StringComparison.OrdinalIgnoreCase));
    }

    // ── D-001 / FR-TASKS-008: IsRecurring=true without RecurrencePattern → 400

    // <REQ-ID: FR-TASKS-008>
    [Fact]
    public async Task PatchTask_IsRecurringTrueWithNullPattern_Returns400()
    {
        var (client, taskId) = await CreateTaskAsync();

        // Explicitly send isRecurring:true but omit recurrencePattern
        var response = await client.PatchAsJsonAsync(
            $"/api/v1/tasks/{taskId}",
            new { IsRecurring = true });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        AssertValidationErrorEnvelope(body);
        var details = body.GetProperty("error").GetProperty("details");
        Assert.Contains(details.EnumerateArray(),
            d => d.GetProperty("field").GetString()!
                  .Equals("RecurrencePattern", StringComparison.OrdinalIgnoreCase));
    }

    // <REQ-ID: FR-TASKS-008>
    [Fact]
    public async Task PatchTask_IsRecurringTrueWithValidPattern_Returns200()
    {
        var (client, taskId) = await CreateTaskAsync();

        // Daily = 0
        var response = await client.PatchAsJsonAsync(
            $"/api/v1/tasks/{taskId}",
            new { IsRecurring = true, RecurrencePattern = 0 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── D-001 / FR-TASKS-008: valid single-field PATCH returns 200 and applies ─

    // <REQ-ID: FR-TASKS-008>
    [Fact]
    public async Task PatchTask_PriorityOnly_Returns200AndAppliesPriority()
    {
        var (client, taskId) = await CreateTaskAsync();

        // Priority enum: Low=0, Medium=1, High=2, Critical=3
        // Task was created with priority=1 (Medium). Patch to Critical (3).
        var response = await client.PatchAsJsonAsync(
            $"/api/v1/tasks/{taskId}",
            new { Priority = 3 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("data", out var data),
            "Success response must have 'data' property.");
        Assert.Equal(3, data.GetProperty("priority").GetInt32());
        // Title must remain unchanged (not overwritten by patch)
        Assert.Equal("Patch Target", data.GetProperty("title").GetString());
    }

    // <REQ-ID: FR-TASKS-008>
    [Fact]
    public async Task PatchTask_StatusOnly_Returns200AndAppliesStatus()
    {
        var (client, taskId) = await CreateTaskAsync();

        // Status: InProgress=1
        var response = await client.PatchAsJsonAsync(
            $"/api/v1/tasks/{taskId}",
            new { Status = 1 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("data").GetProperty("status").GetInt32());
    }

    // <REQ-ID: FR-TASKS-035>
    [Fact]
    public async Task PatchTask_ValidationError_EnvelopeHasErrorKeyNotData()
    {
        var (client, taskId) = await CreateTaskAsync();

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/tasks/{taskId}",
            new { Title = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(body.TryGetProperty("error", out _),
            "400 body must have 'error' key.");
        Assert.False(body.TryGetProperty("data", out _),
            "400 body must NOT have 'data' key.");
    }

    // <REQ-ID: FR-TASKS-035>
    [Fact]
    public async Task PatchTask_Title200Chars_Returns200()
    {
        // Exactly 200 characters is at the boundary — must be accepted.
        var (client, taskId) = await CreateTaskAsync();

        var exactly200 = new string('A', 200);
        var response = await client.PatchAsJsonAsync(
            $"/api/v1/tasks/{taskId}",
            new { Title = exactly200 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(exactly200, body.GetProperty("data").GetProperty("title").GetString());
    }
}
