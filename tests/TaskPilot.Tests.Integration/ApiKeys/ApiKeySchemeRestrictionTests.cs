using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TaskPilot.Tests.Integration.Helpers;

namespace TaskPilot.Tests.Integration.ApiKeys;

/// <summary>
/// WI-APIKEY-SCHEME / NFR-SEC-002
/// Verifies that ApiKeysController is restricted to cookie auth only.
/// An X-Api-Key-authenticated caller must receive 401 on every management
/// endpoint even with a valid key; a cookie-authenticated caller succeeds.
/// These tests FAIL if [Authorize(AuthenticationSchemes=CookieScheme)] is
/// removed from ApiKeysController.
/// </summary>
[Collection("Integration")]
public class ApiKeySchemeRestriction_NFR_SEC_002_Tests : IClassFixture<TaskPilotWebAppFactory>
{
    private readonly TaskPilotWebAppFactory _factory;

    public ApiKeySchemeRestriction_NFR_SEC_002_Tests(TaskPilotWebAppFactory factory)
    {
        _factory = factory;
    }

    // ── helper: build an HttpClient that authenticates only via X-Api-Key ────

    private async Task<(HttpClient ApiKeyClient, string PlainTextKey, string KeyId)>
        CreateApiKeyAuthClientAsync()
    {
        // Step 1 — register a user and generate an API key using cookie auth
        var (cookieClient, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var genResponse = await cookieClient.PostAsJsonAsync("/api/v1/apikeys",
            new { Name = $"SchemeTest_{Guid.NewGuid():N}" });
        genResponse.EnsureSuccessStatusCode();

        var body = await genResponse.Content.ReadFromJsonAsync<JsonElement>();
        var plainTextKey = body.GetProperty("data").GetProperty("plainTextKey").GetString()!;
        var keyId = body.GetProperty("data").GetProperty("id").GetString()!;

        // Step 2 — fresh client using ONLY the API key header (no cookies)
        var apiKeyClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
        apiKeyClient.DefaultRequestHeaders.Add("X-Api-Key", plainTextKey);

        return (apiKeyClient, plainTextKey, keyId);
    }

    // ── GET /api/v1/apikeys ───────────────────────────────────────────────────

    // NFR-SEC-010
    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetApiKeys_ViaApiKeyAuth_Returns401_NFR_SEC_002()
    {
        var (apiKeyClient, _, _) = await CreateApiKeyAuthClientAsync();

        var response = await apiKeyClient.GetAsync("/api/v1/apikeys");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // NFR-SEC-010
    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetApiKeys_ViaCookieAuth_Returns200_NFR_SEC_002()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        await client.PostAsJsonAsync("/api/v1/apikeys", new { Name = $"GetTest_{Guid.NewGuid():N}" });

        var response = await client.GetAsync("/api/v1/apikeys");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("data", out _));
    }

    // ── POST /api/v1/apikeys ─────────────────────────────────────────────────

    // NFR-SEC-010
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateApiKey_ViaApiKeyAuth_Returns401_NFR_SEC_002()
    {
        var (apiKeyClient, _, _) = await CreateApiKeyAuthClientAsync();

        var response = await apiKeyClient.PostAsJsonAsync("/api/v1/apikeys",
            new { Name = "ShouldBeRejected" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // NFR-SEC-010
    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateApiKey_ViaCookieAuth_Returns201_NFR_SEC_002()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.PostAsJsonAsync("/api/v1/apikeys",
            new { Name = $"CookieCreate_{Guid.NewGuid():N}" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // ── DELETE /api/v1/apikeys/{id} ──────────────────────────────────────────

    // NFR-SEC-010
    [Fact]
    [Trait("Category", "Integration")]
    public async Task RevokeApiKey_ViaApiKeyAuth_Returns401_NFR_SEC_002()
    {
        // The keyId obtained from setup references a real key in the DB.
        var (apiKeyClient, _, keyId) = await CreateApiKeyAuthClientAsync();

        var response = await apiKeyClient.DeleteAsync($"/api/v1/apikeys/{keyId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // NFR-SEC-010
    [Fact]
    [Trait("Category", "Integration")]
    public async Task RevokeApiKey_ViaCookieAuth_Returns204_NFR_SEC_002()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var genResponse = await client.PostAsJsonAsync("/api/v1/apikeys",
            new { Name = $"RevokeViaCookie_{Guid.NewGuid():N}" });
        genResponse.EnsureSuccessStatusCode();
        var body = await genResponse.Content.ReadFromJsonAsync<JsonElement>();
        var keyId = body.GetProperty("data").GetProperty("id").GetString()!;

        var response = await client.DeleteAsync($"/api/v1/apikeys/{keyId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ── POST /api/v1/apikeys/{id}/deactivate ────────────────────────────────

    // NFR-SEC-010
    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeactivateApiKey_ViaApiKeyAuth_Returns401_NFR_SEC_002()
    {
        var (apiKeyClient, _, keyId) = await CreateApiKeyAuthClientAsync();

        var response = await apiKeyClient.PostAsync($"/api/v1/apikeys/{keyId}/deactivate", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
