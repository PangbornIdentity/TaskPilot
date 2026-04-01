using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TaskPilot.Data;
using TaskPilot.Entities;
using TaskPilot.Tests.Integration.Helpers;

namespace TaskPilot.Tests.Integration.TaskTypes;

[Collection("Integration")]
public class TaskTypeApiTests : IClassFixture<TaskPilotWebAppFactory>
{
    private readonly TaskPilotWebAppFactory _factory;

    public TaskTypeApiTests(TaskPilotWebAppFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Seeds the 6 standard task types directly into the test database.
    /// These are normally added by migration data seeds which don't run with EnsureCreatedAsync.
    /// </summary>
    private async Task SeedTaskTypesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (!db.TaskTypes.Any())
        {
            db.TaskTypes.AddRange(
                new TaskType { Id = 1, Name = "Task",    SortOrder = 1, IsActive = true },
                new TaskType { Id = 2, Name = "Goal",    SortOrder = 2, IsActive = true },
                new TaskType { Id = 3, Name = "Habit",   SortOrder = 3, IsActive = true },
                new TaskType { Id = 4, Name = "Meeting", SortOrder = 4, IsActive = true },
                new TaskType { Id = 5, Name = "Note",    SortOrder = 5, IsActive = true },
                new TaskType { Id = 6, Name = "Event",   SortOrder = 6, IsActive = true }
            );
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task GetTaskTypes_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/task-types");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTaskTypes_Authenticated_ReturnsSeededList()
    {
        await SeedTaskTypesAsync();
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync("/api/v1/task-types");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");

        // Should contain at least the 6 seeded types
        Assert.True(data.GetArrayLength() >= 6, $"Expected at least 6 task types, got {data.GetArrayLength()}");

        // Verify all 6 seeded names are present
        var names = data.EnumerateArray()
            .Select(t => t.GetProperty("name").GetString())
            .ToList();

        Assert.Contains("Task",    names);
        Assert.Contains("Goal",    names);
        Assert.Contains("Habit",   names);
        Assert.Contains("Meeting", names);
        Assert.Contains("Note",    names);
        Assert.Contains("Event",   names);

        // Verify ordering: each item's sortOrder should be ≤ the next
        var sortOrders = data.EnumerateArray()
            .Select(t => t.GetProperty("sortOrder").GetInt32())
            .ToList();
        for (var i = 1; i < sortOrders.Count; i++)
            Assert.True(sortOrders[i] >= sortOrders[i - 1],
                $"Task types not ordered by sortOrder at index {i}");
    }

    [Fact]
    public async Task GetTaskTypes_Authenticated_AllTypesHaveNameAndId()
    {
        await SeedTaskTypesAsync();
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync("/api/v1/task-types");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");

        foreach (var type in data.EnumerateArray())
        {
            Assert.True(type.TryGetProperty("id", out var id),    "Missing 'id'");
            Assert.True(type.TryGetProperty("name", out var name),"Missing 'name'");
            Assert.True(id.GetInt32() > 0, "id should be positive");
            Assert.False(string.IsNullOrWhiteSpace(name.GetString()), "name should not be empty");
        }
    }
}
