using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TaskPilot.Data;
using TaskPilot.Entities;
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
        TaskTypeId = 1, // Task
        Area = 0, // Personal
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

        var request = new { Title = "", Area = 0, Priority = 1, Status = 0, TargetDateType = 1, IsRecurring = false };
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
            TaskTypeId = 1,
            Area = 0, // Personal
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

    // ── Area, Task Type, and Tag integration tests ─────────────────────────

    [Fact]
    public async Task CreateTask_WithAreaWork_PersistsAndReturnsWork()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var request = new
        {
            Title = $"Work Task {Guid.NewGuid():N}",
            Description = (string?)null,
            TaskTypeId = 1,
            Area = 1, // Work
            Priority = 1,
            Status = 0,
            TargetDateType = 1,
            TargetDate = (DateTime?)null,
            IsRecurring = false,
            RecurrencePattern = (int?)null,
            TagIds = (List<Guid>?)null
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/tasks", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var taskId = created.GetProperty("data").GetProperty("id").GetString();

        // GET it back
        var getResponse = await client.GetAsync($"/api/v1/tasks/{taskId}");
        getResponse.EnsureSuccessStatusCode();
        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        var taskData = body.GetProperty("data");

        Assert.Equal(1, taskData.GetProperty("area").GetInt32());
        Assert.Equal("Work", taskData.GetProperty("areaName").GetString());
    }

    [Fact]
    public async Task CreateTask_WithTaskTypeId_ReturnsTaskTypeName()
    {
        // Seed a task type to use
        var taskTypeId = await EnsureTaskTypeExistsAsync("Goal_Test");

        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var request = new
        {
            Title = $"Typed Task {Guid.NewGuid():N}",
            Description = (string?)null,
            TaskTypeId = taskTypeId,
            Area = 0,
            Priority = 1,
            Status = 0,
            TargetDateType = 1,
            TargetDate = (DateTime?)null,
            IsRecurring = false,
            RecurrencePattern = (int?)null,
            TagIds = (List<Guid>?)null
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/tasks", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var taskId = created.GetProperty("data").GetProperty("id").GetString();

        // The create response may not eagerly load TaskType; GET the task to verify taskTypeName
        var getResponse = await client.GetAsync($"/api/v1/tasks/{taskId}");
        getResponse.EnsureSuccessStatusCode();
        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        var taskData = body.GetProperty("data");

        Assert.Equal(taskTypeId, taskData.GetProperty("taskTypeId").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(taskData.GetProperty("taskTypeName").GetString()),
            "taskTypeName should be populated when retrieved by id");
    }

    [Fact]
    public async Task CreateTask_WithTagIds_ReturnsTagsInResponse()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        // Create a tag first
        var tagName = $"Tag_{Guid.NewGuid():N}";
        var tagResponse = await client.PostAsJsonAsync("/api/v1/tags", new { Name = tagName, Color = "#aabbcc" });
        tagResponse.EnsureSuccessStatusCode();
        var tagBody = await tagResponse.Content.ReadFromJsonAsync<JsonElement>();
        var tagId = Guid.Parse(tagBody.GetProperty("data").GetProperty("id").GetString()!);

        // Create task with the tag
        var request = new
        {
            Title = $"Tagged Task {Guid.NewGuid():N}",
            Description = (string?)null,
            TaskTypeId = 1,
            Area = 0,
            Priority = 1,
            Status = 0,
            TargetDateType = 1,
            TargetDate = (DateTime?)null,
            IsRecurring = false,
            RecurrencePattern = (int?)null,
            TagIds = new List<Guid> { tagId }
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/tasks", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var taskId = created.GetProperty("data").GetProperty("id").GetString();

        // GET task back — tags should appear
        var getResponse = await client.GetAsync($"/api/v1/tasks/{taskId}");
        getResponse.EnsureSuccessStatusCode();
        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        var tags = body.GetProperty("data").GetProperty("tags");

        Assert.True(tags.GetArrayLength() >= 1, "Expected at least one tag in response");
        Assert.Contains(tags.EnumerateArray(),
            t => t.GetProperty("id").GetString() == tagId.ToString());
    }

    [Fact]
    public async Task GetTasks_FilterByArea_ReturnsOnlyMatchingTasks()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var workTitle    = $"WorkTask_{Guid.NewGuid():N}";
        var personalTitle = $"PersonalTask_{Guid.NewGuid():N}";

        // Create one Work task and one Personal task
        await client.PostAsJsonAsync("/api/v1/tasks", MakeCreateRequest(workTitle, area: 1));
        await client.PostAsJsonAsync("/api/v1/tasks", MakeCreateRequest(personalTitle, area: 0));

        // Filter by Work (area=1)
        var response = await client.GetAsync("/api/v1/tasks?area=1");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var tasks = body.GetProperty("data").EnumerateArray().ToList();

        // All returned tasks must have area == 1 (Work)
        Assert.All(tasks, t => Assert.Equal(1, t.GetProperty("area").GetInt32()));
        // The work title should appear, personal should not
        Assert.Contains(tasks, t => t.GetProperty("title").GetString() == workTitle);
        Assert.DoesNotContain(tasks, t => t.GetProperty("title").GetString() == personalTitle);
    }

    [Fact]
    public async Task GetTasks_FilterByTaskTypeId_ReturnsOnlyMatchingTasks()
    {
        var taskTypeId = await EnsureTaskTypeExistsAsync("FilterType_Test");

        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var typedTitle      = $"TypedTask_{Guid.NewGuid():N}";
        var differentTypedTitle = $"DifferentTypeTask_{Guid.NewGuid():N}";

        await client.PostAsJsonAsync("/api/v1/tasks", MakeCreateRequest(typedTitle,         taskTypeId: taskTypeId));
        await client.PostAsJsonAsync("/api/v1/tasks", MakeCreateRequest(differentTypedTitle, taskTypeId: 1)); // Use a different known type (Task)

        var response = await client.GetAsync($"/api/v1/tasks?taskTypeId={taskTypeId}");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var tasks = body.GetProperty("data").EnumerateArray().ToList();

        Assert.All(tasks, t => Assert.Equal(taskTypeId, t.GetProperty("taskTypeId").GetInt32()));
        Assert.Contains(tasks, t => t.GetProperty("title").GetString() == typedTitle);
        Assert.DoesNotContain(tasks, t => t.GetProperty("title").GetString() == differentTypedTitle);
    }

    [Fact]
    public async Task GetTasks_FilterByTagIds_AndLogic_ReturnsOnlyTasksWithAllTags()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        // Create two tags
        var tagAName = $"TagA_{Guid.NewGuid():N}";
        var tagBName = $"TagB_{Guid.NewGuid():N}";

        var tagAResp = await client.PostAsJsonAsync("/api/v1/tags", new { Name = tagAName, Color = "#001122" });
        tagAResp.EnsureSuccessStatusCode();
        var tagAId = Guid.Parse((await tagAResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetString()!);

        var tagBResp = await client.PostAsJsonAsync("/api/v1/tags", new { Name = tagBName, Color = "#334455" });
        tagBResp.EnsureSuccessStatusCode();
        var tagBId = Guid.Parse((await tagBResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetString()!);

        // Task with BOTH tags
        var bothTitle = $"BothTags_{Guid.NewGuid():N}";
        await client.PostAsJsonAsync("/api/v1/tasks", new
        {
            Title = bothTitle,
            Description = (string?)null,
            TaskTypeId = 1,
            Area = 0,
            Priority = 1,
            Status = 0,
            TargetDateType = 1,
            TargetDate = (DateTime?)null,
            IsRecurring = false,
            RecurrencePattern = (int?)null,
            TagIds = new List<Guid> { tagAId, tagBId }
        });

        // Task with only TagA
        var onlyATitle = $"OnlyTagA_{Guid.NewGuid():N}";
        await client.PostAsJsonAsync("/api/v1/tasks", new
        {
            Title = onlyATitle,
            Description = (string?)null,
            TaskTypeId = 1,
            Area = 0,
            Priority = 1,
            Status = 0,
            TargetDateType = 1,
            TargetDate = (DateTime?)null,
            IsRecurring = false,
            RecurrencePattern = (int?)null,
            TagIds = new List<Guid> { tagAId }
        });

        // Filter by both tagIds — AND logic: only tasks with BOTH tags
        var url = $"/api/v1/tasks?tagIds={tagAId}&tagIds={tagBId}";
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var tasks = body.GetProperty("data").EnumerateArray().ToList();

        Assert.Contains(tasks, t => t.GetProperty("title").GetString() == bothTitle);
        Assert.DoesNotContain(tasks, t => t.GetProperty("title").GetString() == onlyATitle);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static object MakeCreateRequest(string title, int area = 0, int taskTypeId = 1) => new
    {
        Title = title,
        Description = (string?)null,
        TaskTypeId = taskTypeId,
        Area = area,
        Priority = 1,
        Status = 0,
        TargetDateType = 1,
        TargetDate = (DateTime?)null,
        IsRecurring = false,
        RecurrencePattern = (int?)null,
        TagIds = (List<Guid>?)null
    };

    private async Task<int> EnsureTaskTypeExistsAsync(string name)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var existing = db.TaskTypes.FirstOrDefault(t => t.Name == name);
        if (existing != null) return existing.Id;

        var taskType = new TaskType { Name = name, SortOrder = 99, IsActive = true };
        db.TaskTypes.Add(taskType);
        await db.SaveChangesAsync();
        return taskType.Id;
    }

    // ───────── Incomplete view + Overdue filter ─────────

    [Fact]
    public async Task GetTasks_WithIncludeOnlyIncomplete_ReturnsOnlyIncompleteStatuses()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        // Status enum: NotStarted=0, InProgress=1, Blocked=2, Completed=3, Cancelled=4
        await client.PostAsJsonAsync("/api/v1/tasks", MakeTask("ns",  status: 0));
        await client.PostAsJsonAsync("/api/v1/tasks", MakeTask("ip",  status: 1));
        await client.PostAsJsonAsync("/api/v1/tasks", MakeTask("blk", status: 2));
        await client.PostAsJsonAsync("/api/v1/tasks", MakeTask("done", status: 3));
        await client.PostAsJsonAsync("/api/v1/tasks", MakeTask("can",  status: 4));

        var response = await client.GetAsync("/api/v1/tasks?includeOnlyIncomplete=true&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data").EnumerateArray().ToList();
        Assert.Equal(3, data.Count);
        Assert.All(data, t =>
        {
            var status = t.GetProperty("status").GetInt32();
            Assert.InRange(status, 0, 2);
        });
    }

    [Fact]
    public async Task GetTasks_WithOverdueOnly_ReturnsOnlyOverdueWithDate()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var yesterday = DateTime.UtcNow.AddDays(-1);
        var tomorrow = DateTime.UtcNow.AddDays(1);

        await client.PostAsJsonAsync("/api/v1/tasks", MakeTask("overdue",      targetDate: yesterday));
        await client.PostAsJsonAsync("/api/v1/tasks", MakeTask("future",       targetDate: tomorrow));
        await client.PostAsJsonAsync("/api/v1/tasks", MakeTask("no-date"));

        var response = await client.GetAsync("/api/v1/tasks?overdueOnly=true&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data").EnumerateArray().ToList();
        Assert.Single(data);
        Assert.Equal("overdue", data[0].GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetTasks_IncompleteAndOverdue_Composes()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var yesterday = DateTime.UtcNow.AddDays(-1);

        await client.PostAsJsonAsync("/api/v1/tasks", MakeTask("notstarted-overdue", status: 0, targetDate: yesterday));
        await client.PostAsJsonAsync("/api/v1/tasks", MakeTask("completed-overdue",  status: 3, targetDate: yesterday));
        await client.PostAsJsonAsync("/api/v1/tasks", MakeTask("blocked-no-date",    status: 2));

        var response = await client.GetAsync(
            "/api/v1/tasks?includeOnlyIncomplete=true&overdueOnly=true&pageSize=50");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data").EnumerateArray().ToList();
        Assert.Single(data);
        Assert.Equal("notstarted-overdue", data[0].GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetStats_IncludesIncompleteByStatus()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        await client.PostAsJsonAsync("/api/v1/tasks", MakeTask("ns1",  status: 0));
        await client.PostAsJsonAsync("/api/v1/tasks", MakeTask("ip1",  status: 1));
        await client.PostAsJsonAsync("/api/v1/tasks", MakeTask("blk1", status: 2));
        await client.PostAsJsonAsync("/api/v1/tasks", MakeTask("done", status: 3));

        var response = await client.GetAsync("/api/v1/tasks/stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var ibs = body.GetProperty("data").GetProperty("incompleteByStatus");
        Assert.Equal(1, ibs.GetProperty("notStarted").GetInt32());
        Assert.Equal(1, ibs.GetProperty("inProgress").GetInt32());
        Assert.Equal(1, ibs.GetProperty("blocked").GetInt32());
        Assert.Equal(3, ibs.GetProperty("total").GetInt32());
    }

    private static object MakeTask(string title, int status = 0, DateTime? targetDate = null) => new
    {
        Title = title,
        Description = (string?)null,
        TaskTypeId = 1,
        Area = 0,
        Priority = 1,
        Status = status,
        TargetDateType = 1,
        TargetDate = targetDate,
        IsRecurring = false,
        RecurrencePattern = (int?)null,
        TagIds = (List<Guid>?)null
    };
}
