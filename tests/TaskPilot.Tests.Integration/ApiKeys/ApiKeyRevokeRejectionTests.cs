using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TaskPilot.Tests.Integration.Helpers;

namespace TaskPilot.Tests.Integration.ApiKeys;

/// <summary>
/// WI-SOFTDELETE (ApiKey) / FR-APIKEYS-004, NFR-DATA-001, NFR-SEC-002
/// Verifies the full pipeline:
///   • DELETE /api/v1/apikeys/{id} returns 204.
///   • A subsequent request using that key's X-Api-Key value is rejected 401.
/// These tests FAIL if RevokeKeyAsync hard-deletes (so GetByHashAsync returns null
/// and the original validate path changes) or if ValidateKeyAsync does not check
/// IsActive on the revoked entity.
/// </summary>
[Collection("Integration")]
public class ApiKeyRevokeRejection_FR_APIKEYS_004_Tests : IClassFixture<TaskPilotWebAppFactory>
{
    private readonly TaskPilotWebAppFactory _factory;

    public ApiKeyRevokeRejection_FR_APIKEYS_004_Tests(TaskPilotWebAppFactory factory)
    {
        _factory = factory;
    }

    // FR-APIKEYS-004
    [Fact]
    [Trait("Category", "Integration")]
    public async Task RevokeKey_ThenUseKey_Returns401_FR_APIKEYS_004()
    {
        // Step 1 — create a user and generate an API key
        var (cookieClient, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var genResponse = await cookieClient.PostAsJsonAsync("/api/v1/apikeys",
            new { Name = $"RevokeTest_{Guid.NewGuid():N}" });
        genResponse.EnsureSuccessStatusCode();

        var genBody = await genResponse.Content.ReadFromJsonAsync<JsonElement>();
        var plainTextKey = genBody.GetProperty("data").GetProperty("plainTextKey").GetString()!;
        var keyId = genBody.GetProperty("data").GetProperty("id").GetString()!;

        // Step 2 — verify the key works before revocation
        var apiClientBefore = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
        apiClientBefore.DefaultRequestHeaders.Add("X-Api-Key", plainTextKey);

        var beforeResp = await apiClientBefore.GetAsync("/api/v1/tasks");
        Assert.Equal(HttpStatusCode.OK, beforeResp.StatusCode);

        // Step 3 — revoke the key via cookie auth
        var deleteResp = await cookieClient.DeleteAsync($"/api/v1/apikeys/{keyId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        // Step 4 — verify the revoked key no longer authenticates
        var apiClientAfter = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
        apiClientAfter.DefaultRequestHeaders.Add("X-Api-Key", plainTextKey);

        var afterResp = await apiClientAfter.GetAsync("/api/v1/tasks");
        Assert.Equal(HttpStatusCode.Unauthorized, afterResp.StatusCode);
    }

    // NFR-DATA-001
    [Fact]
    [Trait("Category", "Integration")]
    public async Task RevokeKey_NoLongerInGetKeysList_NFR_DATA_001()
    {
        // Soft-delete means the key must disappear from the user's key list.
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var genResponse = await client.PostAsJsonAsync("/api/v1/apikeys",
            new { Name = $"ListCheck_{Guid.NewGuid():N}" });
        genResponse.EnsureSuccessStatusCode();

        var keyId = (await genResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data").GetProperty("id").GetString()!;

        await client.DeleteAsync($"/api/v1/apikeys/{keyId}");

        var listResp = await client.GetAsync("/api/v1/apikeys");
        listResp.EnsureSuccessStatusCode();
        var list = (await listResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("data");

        var stillPresent = list.EnumerateArray().Any(k => k.GetProperty("id").GetString() == keyId);
        Assert.False(stillPresent, "Soft-deleted key must not appear in GET /api/v1/apikeys.");
    }
}
