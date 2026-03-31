using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TaskPilot.Tests.Integration.Helpers;

namespace TaskPilot.Tests.Integration.ActivityLogs;

[Collection("Integration")]
public class ActivityLogApiTests : IClassFixture<TaskPilotWebAppFactory>
{
    private readonly TaskPilotWebAppFactory _factory;

    public ActivityLogApiTests(TaskPilotWebAppFactory factory)
    {
        _factory = factory;
    }

    private static object ValidCreateTaskRequest(string title = "Test Task") => new
    {
        Title = title,
        Description = (string?)null,
        Type = "Work",
        Priority = 1, // Medium
        Status = 0,   // NotStarted
        TargetDateType = 1, // ThisWeek
        TargetDate = (DateTime?)null,
        IsRecurring = false,
        RecurrencePattern = (int?)null,
        TagIds = (List<Guid>?)null
    };

    [Fact]
    public async Task GetActivityLogs_UnauthenticatedUser_Returns401()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/v1/activity-logs");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetActivityLogs_NewUser_ReturnsEmptyList()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync("/api/v1/activity-logs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, body.GetProperty("data").GetArrayLength());
        Assert.Equal(0, body.GetProperty("meta").GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task GetActivityLogs_AfterTaskUpdate_ContainsLog()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        // Create a task
        var createResponse = await client.PostAsJsonAsync("/api/v1/tasks", ValidCreateTaskRequest("Audit Test Task"));
        createResponse.EnsureSuccessStatusCode();
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var taskId = createBody.GetProperty("data").GetProperty("id").GetString()!;

        // Update it to generate an activity log
        var updateResponse = await client.PutAsJsonAsync($"/api/v1/tasks/{taskId}", new
        {
            Title = "Updated Title",
            Type = "Work",
            Priority = 1,
            Status = 0,
            TargetDateType = 1,
            IsRecurring = false
        });
        updateResponse.EnsureSuccessStatusCode();

        // Query activity logs
        var response = await client.GetAsync("/api/v1/activity-logs");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");
        Assert.True(data.GetArrayLength() > 0, "Expected at least one activity log after task update");
    }

    [Fact]
    public async Task GetActivityLogs_FilterByTaskId_ReturnsOnlyThatTasksLogs()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        // Create two tasks
        var r1 = await client.PostAsJsonAsync("/api/v1/tasks", ValidCreateTaskRequest("Task One"));
        var b1 = await r1.Content.ReadFromJsonAsync<JsonElement>();
        var taskId1 = b1.GetProperty("data").GetProperty("id").GetString()!;

        var r2 = await client.PostAsJsonAsync("/api/v1/tasks", ValidCreateTaskRequest("Task Two"));
        var b2 = await r2.Content.ReadFromJsonAsync<JsonElement>();
        var taskId2 = b2.GetProperty("data").GetProperty("id").GetString()!;

        // Update both
        await client.PutAsJsonAsync($"/api/v1/tasks/{taskId1}", new
        {
            Title = "Task One Updated", Type = "Work", Priority = 1, Status = 0, TargetDateType = 1, IsRecurring = false
        });
        await client.PutAsJsonAsync($"/api/v1/tasks/{taskId2}", new
        {
            Title = "Task Two Updated", Type = "Work", Priority = 1, Status = 0, TargetDateType = 1, IsRecurring = false
        });

        // Filter by taskId1
        var response = await client.GetAsync($"/api/v1/activity-logs?taskId={taskId1}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");

        // All returned logs should be for taskId1
        foreach (var log in data.EnumerateArray())
            Assert.Equal(taskId1, log.GetProperty("taskId").GetString());
    }

    [Fact]
    public async Task GetActivityLogs_ReturnsPagedMetadata()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync("/api/v1/activity-logs?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var meta = body.GetProperty("meta");
        Assert.Equal(1, meta.GetProperty("page").GetInt32());
        Assert.Equal(10, meta.GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task GetActivityLogs_AfterTaskCreate_ContainsCreatedLog()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var createResponse = await client.PostAsJsonAsync("/api/v1/tasks", ValidCreateTaskRequest("Created Log Test"));
        createResponse.EnsureSuccessStatusCode();
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var taskId = createBody.GetProperty("data").GetProperty("id").GetString()!;

        var response = await client.GetAsync($"/api/v1/activity-logs?taskId={taskId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data").EnumerateArray().ToList();
        Assert.True(data.Any(l => l.GetProperty("fieldChanged").GetString() == "Created"),
            "Expected a 'Created' activity log entry after task creation");
    }

    [Fact]
    public async Task GetActivityLogs_AfterTaskDelete_DeletedLogStillVisible()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        // Create a task
        var createResponse = await client.PostAsJsonAsync("/api/v1/tasks", ValidCreateTaskRequest("Delete Log Test"));
        createResponse.EnsureSuccessStatusCode();
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var taskId = createBody.GetProperty("data").GetProperty("id").GetString()!;

        // Delete it
        var deleteResponse = await client.DeleteAsync($"/api/v1/tasks/{taskId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Logs for deleted tasks should still be visible (IgnoreQueryFilters on join)
        var response = await client.GetAsync($"/api/v1/activity-logs?taskId={taskId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data").EnumerateArray().ToList();
        Assert.True(data.Any(l => l.GetProperty("fieldChanged").GetString() == "Deleted"),
            "Expected a 'Deleted' activity log entry visible even after soft-delete");
    }

    [Fact]
    public async Task GetActivityLogs_LogContainsExpectedFields()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        // Create and update a task to generate a log
        var createResponse = await client.PostAsJsonAsync("/api/v1/tasks", ValidCreateTaskRequest("Fields Test"));
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var taskId = createBody.GetProperty("data").GetProperty("id").GetString()!;

        await client.PutAsJsonAsync($"/api/v1/tasks/{taskId}", new
        {
            Title = "Fields Test Updated", Type = "Work", Priority = 1, Status = 0, TargetDateType = 1, IsRecurring = false
        });

        var response = await client.GetAsync($"/api/v1/activity-logs?taskId={taskId}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var log = body.GetProperty("data").EnumerateArray().First();

        Assert.True(log.TryGetProperty("id", out _));
        Assert.True(log.TryGetProperty("taskId", out _));
        Assert.True(log.TryGetProperty("taskTitle", out _));
        Assert.True(log.TryGetProperty("timestamp", out _));
        Assert.True(log.TryGetProperty("fieldChanged", out _));
        Assert.True(log.TryGetProperty("changedBy", out _));
    }
}
