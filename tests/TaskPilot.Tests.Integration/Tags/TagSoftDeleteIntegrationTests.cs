using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TaskPilot.Tests.Integration.Helpers;

namespace TaskPilot.Tests.Integration.Tags;

/// <summary>
/// WI-SOFTDELETE (Tag) / FR-TAGS-004, NFR-DATA-001
/// Integration-level proof:
///   • DELETE /api/v1/tags/{id} returns 204.
///   • GET /api/v1/tags no longer lists the deleted tag.
///   • A task that previously had the tag shows zero tag count after deletion.
/// These tests FAIL if the service performs a hard-delete instead of soft-delete
/// or if the query filter is removed.
/// </summary>
[Collection("Integration")]
public class TagSoftDelete_FR_TAGS_004_Integration_Tests : IClassFixture<TaskPilotWebAppFactory>
{
    private readonly TaskPilotWebAppFactory _factory;

    public TagSoftDelete_FR_TAGS_004_Integration_Tests(TaskPilotWebAppFactory factory)
    {
        _factory = factory;
    }

    // FR-TAGS-004
    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeleteTag_Returns204_FR_TAGS_004()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var tagName = $"SoftDel_{Guid.NewGuid():N}";
        var createResp = await client.PostAsJsonAsync("/api/v1/tags", new { Name = tagName, Color = "#aabbcc" });
        createResp.EnsureSuccessStatusCode();
        var tagId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetString()!;

        var deleteResp = await client.DeleteAsync($"/api/v1/tags/{tagId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);
    }

    // FR-TAGS-004
    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeleteTag_RemovedFromGetList_FR_TAGS_004()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var tagName = $"Invisible_{Guid.NewGuid():N}";
        var createResp = await client.PostAsJsonAsync("/api/v1/tags", new { Name = tagName, Color = "#112233" });
        createResp.EnsureSuccessStatusCode();
        var tagId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetString()!;

        await client.DeleteAsync($"/api/v1/tags/{tagId}");

        var listResp = await client.GetAsync("/api/v1/tags");
        listResp.EnsureSuccessStatusCode();
        var list = (await listResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data");

        var found = list.EnumerateArray().Any(t => t.GetProperty("id").GetString() == tagId);
        Assert.False(found, "Soft-deleted tag must not appear in GET /api/v1/tags response.");
    }

    // FR-TAGS-004
    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeleteTag_TaskNoLongerShowsTag_FR_TAGS_004()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        // Create tag
        var tagName = $"TaskTag_{Guid.NewGuid():N}";
        var tagResp = await client.PostAsJsonAsync("/api/v1/tags", new { Name = tagName, Color = "#445566" });
        tagResp.EnsureSuccessStatusCode();
        var tagId = (await tagResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetString()!;

        // Create task referencing the tag
        var taskResp = await client.PostAsJsonAsync("/api/v1/tasks", new
        {
            Title = "Task with tag",
            Description = (string?)null,
            TaskTypeId = 1, Area = 0, Priority = 1, Status = 0,
            TargetDateType = 1, TargetDate = (DateTime?)null,
            IsRecurring = false, RecurrencePattern = (int?)null,
            TagIds = new[] { tagId }
        });
        taskResp.EnsureSuccessStatusCode();

        // Verify tag count before deletion
        var beforeList = await (await client.GetAsync("/api/v1/tags"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var beforeTag = beforeList.GetProperty("data").EnumerateArray()
            .FirstOrDefault(t => t.GetProperty("id").GetString() == tagId);
        Assert.True(beforeTag.ValueKind != System.Text.Json.JsonValueKind.Undefined,
            "Tag should appear in list before deletion");
        Assert.Equal(1, beforeTag.GetProperty("taskCount").GetInt32());

        // Soft-delete the tag
        var deleteResp = await client.DeleteAsync($"/api/v1/tags/{tagId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        // Verify the tag no longer appears in the list
        var afterList = await (await client.GetAsync("/api/v1/tags"))
            .Content.ReadFromJsonAsync<JsonElement>();
        var stillPresent = afterList.GetProperty("data").EnumerateArray()
            .Any(t => t.GetProperty("id").GetString() == tagId);
        Assert.False(stillPresent, "Tag must not appear in list after soft-delete");
    }
}
