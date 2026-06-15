using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TaskPilot.Tests.Integration.Helpers;

namespace TaskPilot.Tests.Integration.Tags;

/// <summary>
/// BIZ-TAGS-001 — integration coverage for the resurrect-on-recreate fix.
///
/// A soft-deleted tag's (UserId, Name) row occupies the unfiltered unique index.
/// Without the resurrect path, POST /api/v1/tags with the same name after a DELETE
/// passes the service-level duplicate check (which uses the query-filtered view) but
/// then hits a DbUpdateException from the DB unique constraint → HTTP 500.
///
/// These tests ensure:
///   • Re-creating a just-soft-deleted tag returns 201 (not 500) and the tag is live again.
///   • Creating a name that matches a LIVE tag still returns 409.
/// </summary>
[Collection("Integration")]
public class TagResurrect_BIZ_TAGS_001_Integration_Tests : IClassFixture<TaskPilotWebAppFactory>
{
    private readonly TaskPilotWebAppFactory _factory;

    public TagResurrect_BIZ_TAGS_001_Integration_Tests(TaskPilotWebAppFactory factory)
    {
        _factory = factory;
    }

    // BIZ-TAGS-001
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateTag_AfterSoftDelete_SameName_Returns201_BIZ_TAGS_001()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var tagName = $"Resurrect_{Guid.NewGuid():N}";

        // Create then soft-delete the tag
        var createResp = await client.PostAsJsonAsync("/api/v1/tags", new { Name = tagName, Color = "#111111" });
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var tagId = created.GetProperty("data").GetProperty("id").GetString()!;

        var deleteResp = await client.DeleteAsync($"/api/v1/tags/{tagId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        // Recreate with the same name — must be 201, not 500
        var recreateResp = await client.PostAsJsonAsync("/api/v1/tags", new { Name = tagName, Color = "#222222" });
        Assert.Equal(HttpStatusCode.Created, recreateResp.StatusCode);

        var recreated = await recreateResp.Content.ReadFromJsonAsync<JsonElement>();
        var data = recreated.GetProperty("data");
        Assert.Equal(tagName, data.GetProperty("name").GetString());
        // Color was updated to the new value on resurrection
        Assert.Equal("#222222", data.GetProperty("color").GetString());
    }

    // BIZ-TAGS-001
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateTag_AfterSoftDelete_AppearsInGetList_BIZ_TAGS_001()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var tagName = $"ResurrectList_{Guid.NewGuid():N}";

        var createResp = await client.PostAsJsonAsync("/api/v1/tags", new { Name = tagName, Color = "#aabbcc" });
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var tagId = created.GetProperty("data").GetProperty("id").GetString()!;

        await client.DeleteAsync($"/api/v1/tags/{tagId}");

        // Resurrect
        var recreateResp = await client.PostAsJsonAsync("/api/v1/tags", new { Name = tagName, Color = "#ddeeff" });
        recreateResp.EnsureSuccessStatusCode();

        // Tag must now appear in GET /api/v1/tags
        var listResp = await client.GetAsync("/api/v1/tags");
        listResp.EnsureSuccessStatusCode();
        var list = await listResp.Content.ReadFromJsonAsync<JsonElement>();
        var found = list.GetProperty("data").EnumerateArray()
            .Any(t => t.GetProperty("name").GetString() == tagName);
        Assert.True(found, "Resurrected tag must appear in GET /api/v1/tags");
    }

    // BIZ-TAGS-001
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateTag_LiveDuplicate_StillReturns409_BIZ_TAGS_001()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var tagName = $"LiveDup_{Guid.NewGuid():N}";

        var firstResp = await client.PostAsJsonAsync("/api/v1/tags", new { Name = tagName, Color = "#000000" });
        firstResp.EnsureSuccessStatusCode();

        // Same name against a live (not deleted) tag must still be 409
        var dupResp = await client.PostAsJsonAsync("/api/v1/tags", new { Name = tagName, Color = "#ffffff" });
        Assert.Equal(HttpStatusCode.Conflict, dupResp.StatusCode);
    }
}
