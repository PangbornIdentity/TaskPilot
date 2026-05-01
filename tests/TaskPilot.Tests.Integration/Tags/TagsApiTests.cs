using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TaskPilot.Tests.Integration.Helpers;

namespace TaskPilot.Tests.Integration.Tags;

[Collection("Integration")]
public class TagsApiTests : IClassFixture<TaskPilotWebAppFactory>
{
    private readonly TaskPilotWebAppFactory _factory;

    public TagsApiTests(TaskPilotWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateTag_ValidRequest_Returns201()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.PostAsJsonAsync("/api/v1/tags", new
        {
            Name = $"Tag_{Guid.NewGuid():N}",
            Color = "#6255EC"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("data", out var data));
        Assert.Equal("#6255EC", data.GetProperty("color").GetString());
    }

    [Fact]
    public async Task CreateTag_DuplicateName_Returns409()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var tagName = $"Dup_{Guid.NewGuid():N}";
        var tagRequest = new { Name = tagName, Color = "#ff0000" };

        await client.PostAsJsonAsync("/api/v1/tags", tagRequest);
        var response = await client.PostAsJsonAsync("/api/v1/tags", tagRequest);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetTags_ReturnsAllUserTags()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        await client.PostAsJsonAsync("/api/v1/tags", new { Name = $"Tag1_{Guid.NewGuid():N}", Color = "#aabbcc" });

        var response = await client.GetAsync("/api/v1/tags");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("data", out var data));
        Assert.True(data.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task DeleteTag_ExistingTag_Returns204()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var createResponse = await client.PostAsJsonAsync("/api/v1/tags", new
        {
            Name = $"DeleteMe_{Guid.NewGuid():N}",
            Color = "#112233"
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var tagId = created.GetProperty("data").GetProperty("id").GetString();

        var response = await client.DeleteAsync($"/api/v1/tags/{tagId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTag_OtherUsersTag_Returns404()
    {
        var (client1, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var createResponse = await client1.PostAsJsonAsync("/api/v1/tags", new
        {
            Name = $"User1Tag_{Guid.NewGuid():N}",
            Color = "#aabbcc"
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var tagId = created.GetProperty("data").GetProperty("id").GetString();

        var (client2, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var response = await client2.DeleteAsync($"/api/v1/tags/{tagId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTag_ValidRequest_Returns200WithUpdatedTag()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var originalName = $"Renamable_{Guid.NewGuid():N}";
        var createResponse = await client.PostAsJsonAsync("/api/v1/tags",
            new { Name = originalName, Color = "#000000" });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var tagId = created.GetProperty("data").GetProperty("id").GetString();

        var updatedName = originalName + "_updated";
        var response = await client.PutAsJsonAsync($"/api/v1/tags/{tagId}",
            new { Name = updatedName, Color = "#AABBCC" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");
        Assert.Equal(updatedName, data.GetProperty("name").GetString());
        Assert.Equal("#AABBCC", data.GetProperty("color").GetString());
    }

    [Fact]
    public async Task UpdateTag_NonExistentTag_Returns404()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.PutAsJsonAsync($"/api/v1/tags/{Guid.NewGuid()}",
            new { Name = "Anything", Color = "#000000" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTag_OtherUsersTag_Returns404()
    {
        var (client1, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var createResponse = await client1.PostAsJsonAsync("/api/v1/tags",
            new { Name = $"OwnedByUser1_{Guid.NewGuid():N}", Color = "#000000" });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var tagId = created.GetProperty("data").GetProperty("id").GetString();

        var (client2, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var response = await client2.PutAsJsonAsync($"/api/v1/tags/{tagId}",
            new { Name = "Hijacked", Color = "#000000" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTag_DuplicateNameForSameUser_Returns409()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var nameA = $"TagA_{Guid.NewGuid():N}";
        var nameB = $"TagB_{Guid.NewGuid():N}";
        await client.PostAsJsonAsync("/api/v1/tags", new { Name = nameA, Color = "#111111" });
        var resp = await client.PostAsJsonAsync("/api/v1/tags", new { Name = nameB, Color = "#222222" });
        var tagB = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var tagBId = tagB.GetProperty("data").GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync($"/api/v1/tags/{tagBId}",
            new { Name = nameA, Color = "#222222" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTag_InvalidPayload_Returns400()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var createResponse = await client.PostAsJsonAsync("/api/v1/tags",
            new { Name = $"ToInvalidate_{Guid.NewGuid():N}", Color = "#000000" });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var tagId = created.GetProperty("data").GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync($"/api/v1/tags/{tagId}",
            new { Name = string.Empty, Color = "not-a-hex" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTag_Unauthenticated_Returns401()
    {
        var anon = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await anon.PutAsJsonAsync($"/api/v1/tags/{Guid.NewGuid()}",
            new { Name = "x", Color = "#000000" });

        Assert.True(response.StatusCode is HttpStatusCode.Unauthorized
                                         or HttpStatusCode.Redirect
                                         or HttpStatusCode.Found);
    }

    [Fact]
    public async Task GetTags_TagAssignedToTask_PopulatesTaskCount()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var tagResp = await client.PostAsJsonAsync("/api/v1/tags",
            new { Name = $"Counted_{Guid.NewGuid():N}", Color = "#111111" });
        tagResp.EnsureSuccessStatusCode();
        var tag = await tagResp.Content.ReadFromJsonAsync<JsonElement>();
        var tagId = tag.GetProperty("data").GetProperty("id").GetString();

        await client.PostAsJsonAsync("/api/v1/tasks", new
        {
            Title = "task with the tag",
            Description = (string?)null,
            TaskTypeId = 1, Area = 0, Priority = 1, Status = 0,
            TargetDateType = 1, TargetDate = (DateTime?)null,
            IsRecurring = false, RecurrencePattern = (int?)null,
            TagIds = new[] { tagId }
        });

        var listResp = await client.GetAsync("/api/v1/tags");
        var body = await listResp.Content.ReadFromJsonAsync<JsonElement>();
        var match = body.GetProperty("data").EnumerateArray()
            .First(t => t.GetProperty("id").GetString() == tagId);

        Assert.Equal(1, match.GetProperty("taskCount").GetInt32());
    }
}
