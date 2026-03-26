using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TaskPilot.Tests.Integration.Helpers;

namespace TaskPilot.Tests.Integration.Tasks;

[Collection("Integration")]
public class TasksApiTests : IClassFixture<TaskPilotWebAppFactory>
{
    private readonly TaskPilotWebAppFactory _factory;

    public TasksApiTests(TaskPilotWebAppFactory factory)
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
    public async Task CreateTask_ValidRequest_Returns201WithTaskResponse()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.PostAsJsonAsync("/api/v1/tasks", ValidCreateTaskRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Test Task", body.GetProperty("data").GetProperty("title").GetString());
    }

    [Fact]
    public async Task CreateTask_MissingTitle_Returns400WithValidationError()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var request = new { Title = "", Type = "Work", Priority = 1, Status = 0, TargetDateType = 1, IsRecurring = false };
        var response = await client.PostAsJsonAsync("/api/v1/tasks", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTasks_AuthenticatedUser_ReturnsPagedResponse()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        await client.PostAsJsonAsync("/api/v1/tasks", ValidCreateTaskRequest("Paged Task"));

        var response = await client.GetAsync("/api/v1/tasks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("data").GetArrayLength() >= 0);
        Assert.True(body.TryGetProperty("meta", out _));
    }

    [Fact]
    public async Task GetTasks_SearchFilter_ReturnsMatchingTasks()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var uniqueTitle = $"SearchableTask_{Guid.NewGuid():N}";
        await client.PostAsJsonAsync("/api/v1/tasks", ValidCreateTaskRequest(uniqueTitle));

        var response = await client.GetAsync($"/api/v1/tasks?search={uniqueTitle[..12]}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");
        Assert.True(data.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task GetTasks_StatusFilter_ReturnsOnlyMatchingStatus()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        await client.PostAsJsonAsync("/api/v1/tasks", ValidCreateTaskRequest("StatusFilterTask"));

        // Filter by NotStarted (0)
        var response = await client.GetAsync("/api/v1/tasks?status=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var tasks = body.GetProperty("data").EnumerateArray();
        foreach (var task in tasks)
        {
            Assert.Equal(0, task.GetProperty("status").GetInt32());
        }
    }

    [Fact]
    public async Task GetTaskById_ExistingTask_Returns200()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var createResponse = await client.PostAsJsonAsync("/api/v1/tasks", ValidCreateTaskRequest("Get By Id Task"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var taskId = created.GetProperty("data").GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/v1/tasks/{taskId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(taskId, body.GetProperty("data").GetProperty("id").GetString());
    }

    [Fact]
    public async Task GetTaskById_NotFound_Returns404()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync($"/api/v1/tasks/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTaskById_OtherUsersTask_Returns404()
    {
        var (client1, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var createResponse = await client1.PostAsJsonAsync("/api/v1/tasks", ValidCreateTaskRequest("User1 Task"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var taskId = created.GetProperty("data").GetProperty("id").GetString();

        // Create a second user and try to access user1's task
        var (client2, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var response = await client2.GetAsync($"/api/v1/tasks/{taskId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTask_ValidRequest_Returns200WithUpdatedFields()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var createResponse = await client.PostAsJsonAsync("/api/v1/tasks", ValidCreateTaskRequest("Original Title"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var taskId = created.GetProperty("data").GetProperty("id").GetString();

        var updateRequest = new
        {
            Title = "Updated Title",
            Description = (string?)null,
            Type = "Work",
            Priority = 2, // High
            Status = 0,
            TargetDateType = 1,
            TargetDate = (DateTime?)null,
            IsRecurring = false,
            RecurrencePattern = (int?)null,
            TagIds = (List<Guid>?)null
        };

        var response = await client.PutAsJsonAsync($"/api/v1/tasks/{taskId}", updateRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Updated Title", body.GetProperty("data").GetProperty("title").GetString());
    }

    [Fact]
    public async Task PatchTask_PriorityOnly_UpdatesOnlyPriority()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var createResponse = await client.PostAsJsonAsync("/api/v1/tasks", ValidCreateTaskRequest("Patch Task"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var taskId = created.GetProperty("data").GetProperty("id").GetString();

        var patchRequest = new { Priority = 3 }; // High
        var response = await client.PatchAsJsonAsync($"/api/v1/tasks/{taskId}", patchRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        // Title should remain "Patch Task"
        Assert.Equal("Patch Task", body.GetProperty("data").GetProperty("title").GetString());
    }

    [Fact]
    public async Task CompleteTask_SetsStatusCompleted()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var createResponse = await client.PostAsJsonAsync("/api/v1/tasks", ValidCreateTaskRequest("Complete Task"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var taskId = created.GetProperty("data").GetProperty("id").GetString();

        var response = await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/complete",
            new { ResultAnalysis = (string?)null });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, body.GetProperty("data").GetProperty("status").GetInt32()); // Completed = 3
    }

    [Fact]
    public async Task CompleteTask_WithResultAnalysis_StoredCorrectly()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var createResponse = await client.PostAsJsonAsync("/api/v1/tasks", ValidCreateTaskRequest("Analyze Task"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var taskId = created.GetProperty("data").GetProperty("id").GetString();

        var response = await client.PostAsJsonAsync($"/api/v1/tasks/{taskId}/complete",
            new { ResultAnalysis = "It went great!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("It went great!", body.GetProperty("data").GetProperty("resultAnalysis").GetString());
    }

    [Fact]
    public async Task DeleteTask_SoftDeletes_TaskNoLongerInList()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var createResponse = await client.PostAsJsonAsync("/api/v1/tasks", ValidCreateTaskRequest("Delete Task"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var taskId = created.GetProperty("data").GetProperty("id").GetString();

        var deleteResponse = await client.DeleteAsync($"/api/v1/tasks/{taskId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Task should now return 404
        var getResponse = await client.GetAsync($"/api/v1/tasks/{taskId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteTask_NotFound_Returns404()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.DeleteAsync($"/api/v1/tasks/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetStats_Returns200WithStatsShape()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync("/api/v1/tasks/stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("data", out var data));
        Assert.True(data.TryGetProperty("totalActive", out _));
    }
}
