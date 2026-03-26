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
}
