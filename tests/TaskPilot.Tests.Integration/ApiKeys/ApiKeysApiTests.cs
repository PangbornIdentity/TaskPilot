using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TaskPilot.Tests.Integration.Helpers;

namespace TaskPilot.Tests.Integration.ApiKeys;

[Collection("Integration")]
public class ApiKeysApiTests : IClassFixture<TaskPilotWebAppFactory>
{
    private readonly TaskPilotWebAppFactory _factory;

    public ApiKeysApiTests(TaskPilotWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GenerateKey_ValidName_Returns201WithPlainTextKey()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.PostAsJsonAsync("/api/v1/apikeys", new { Name = "My Key" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var plainTextKey = body.GetProperty("data").GetProperty("plainTextKey").GetString();
        Assert.False(string.IsNullOrEmpty(plainTextKey));
        Assert.True(plainTextKey!.Length >= 8);
    }

    [Fact]
    public async Task GenerateKey_PlainTextKeyNotStoredInDb()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.PostAsJsonAsync("/api/v1/apikeys", new { Name = "Hash Verify Key" });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var plainTextKey = body.GetProperty("data").GetProperty("plainTextKey").GetString()!;
        var keyPrefix = body.GetProperty("data").GetProperty("keyPrefix").GetString()!;

        // Verify prefix matches first 8 chars of plain text
        Assert.Equal(plainTextKey[..8], keyPrefix);

        // Get all keys — plain text should not be visible
        var keysResponse = await client.GetAsync("/api/v1/apikeys");
        var keysBody = await keysResponse.Content.ReadFromJsonAsync<JsonElement>();
        var keys = keysBody.GetProperty("data").EnumerateArray();
        foreach (var key in keys)
        {
            // Response DTO should not contain plainTextKey
            Assert.False(key.TryGetProperty("plainTextKey", out _));
            Assert.False(key.TryGetProperty("keyHash", out _));
        }
    }

    [Fact]
    public async Task GetKeys_ReturnsAllUserKeys()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        await client.PostAsJsonAsync("/api/v1/apikeys", new { Name = "List Key 1" });
        await client.PostAsJsonAsync("/api/v1/apikeys", new { Name = "List Key 2" });

        var response = await client.GetAsync("/api/v1/apikeys");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("data").GetArrayLength() >= 2);
    }

    [Fact]
    public async Task DeactivateKey_KeyBecomesInactive()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var genResponse = await client.PostAsJsonAsync("/api/v1/apikeys", new { Name = "Deactivate Me" });
        genResponse.EnsureSuccessStatusCode();
        var genBody = await genResponse.Content.ReadFromJsonAsync<JsonElement>();
        var keyId = genBody.GetProperty("data").GetProperty("id").GetString()!;

        var deactivateResponse = await client.PostAsync($"/api/v1/apikeys/{keyId}/deactivate", null);
        Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);

        // Verify key is now inactive
        var keysResponse = await client.GetAsync("/api/v1/apikeys");
        var keysBody = await keysResponse.Content.ReadFromJsonAsync<JsonElement>();
        var key = keysBody.GetProperty("data").EnumerateArray()
            .FirstOrDefault(k => k.GetProperty("id").GetString() == keyId);

        Assert.False(key.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task ActivateKey_InactiveKeyBecomesActive()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var genResponse = await client.PostAsJsonAsync("/api/v1/apikeys", new { Name = "Activate Me" });
        genResponse.EnsureSuccessStatusCode();
        var genBody = await genResponse.Content.ReadFromJsonAsync<JsonElement>();
        var keyId = genBody.GetProperty("data").GetProperty("id").GetString()!;

        // First deactivate
        await client.PostAsync($"/api/v1/apikeys/{keyId}/deactivate", null);

        // Then activate
        var activateResponse = await client.PostAsync($"/api/v1/apikeys/{keyId}/activate", null);
        Assert.Equal(HttpStatusCode.NoContent, activateResponse.StatusCode);

        // Verify key is active
        var keysResponse = await client.GetAsync("/api/v1/apikeys");
        var keysBody = await keysResponse.Content.ReadFromJsonAsync<JsonElement>();
        var key = keysBody.GetProperty("data").EnumerateArray()
            .FirstOrDefault(k => k.GetProperty("id").GetString() == keyId);

        Assert.True(key.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task RevokeKey_KeyDeleted()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var genResponse = await client.PostAsJsonAsync("/api/v1/apikeys", new { Name = "Revoke Me" });
        genResponse.EnsureSuccessStatusCode();
        var genBody = await genResponse.Content.ReadFromJsonAsync<JsonElement>();
        var keyId = genBody.GetProperty("data").GetProperty("id").GetString()!;

        var revokeResponse = await client.DeleteAsync($"/api/v1/apikeys/{keyId}");
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

        // Verify key is gone
        var keysResponse = await client.GetAsync("/api/v1/apikeys");
        var keysBody = await keysResponse.Content.ReadFromJsonAsync<JsonElement>();
        var keyExists = keysBody.GetProperty("data").EnumerateArray()
            .Any(k => k.GetProperty("id").GetString() == keyId);

        Assert.False(keyExists);
    }
}
