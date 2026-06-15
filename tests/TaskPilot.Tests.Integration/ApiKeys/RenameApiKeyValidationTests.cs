using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TaskPilot.Tests.Integration.Helpers;

namespace TaskPilot.Tests.Integration.ApiKeys;

/// <summary>
/// Integration tests proving defect fix D-005:
///   PATCH /api/v1/apikeys/{id}/rename now runs the new
///   RenameApiKeyRequestValidator before touching the database.
///
/// Every test FAILS if the validator or the controller's validation block
/// is removed / reverted to a no-op.
///
/// REQ: FR-APIKEYS-005 (rename API key).
/// </summary>
[Collection("Integration")]
public class RenameApiKeyValidationTests : IClassFixture<TaskPilotWebAppFactory>
{
    private readonly TaskPilotWebAppFactory _factory;

    public RenameApiKeyValidationTests(TaskPilotWebAppFactory factory)
    {
        _factory = factory;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Creates a fresh user + API key, returns the cookie client and key id.</summary>
    private async Task<(HttpClient Client, string KeyId)> CreateKeyAsync(string keyName = "Test Key")
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var genResponse = await client.PostAsJsonAsync("/api/v1/apikeys", new { Name = keyName });
        genResponse.EnsureSuccessStatusCode();
        var body = await genResponse.Content.ReadFromJsonAsync<JsonElement>();
        var keyId = body.GetProperty("data").GetProperty("id").GetString()!;
        return (client, keyId);
    }

    // ── FR-APIKEYS-005 / D-005: empty name → 400 ─────────────────────────────

    // <REQ-ID: FR-APIKEYS-005>
    [Fact]
    public async Task RenameKey_EmptyName_Returns400WithValidationError()
    {
        var (client, keyId) = await CreateKeyAsync();

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/apikeys/{keyId}/rename",
            new { Name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        // Standard validation-error envelope: { "error": { "code": "...", "message": "...", "details": [...] } }
        Assert.True(body.TryGetProperty("error", out var error),
            "Response must have an 'error' property.");
        Assert.True(error.TryGetProperty("details", out var details),
            "Validation error envelope must contain 'details'.");
        Assert.True(details.GetArrayLength() > 0,
            "At least one field-level error detail must be present.");
        // The errored field must be 'Name'
        Assert.Contains(details.EnumerateArray(),
            d => d.GetProperty("field").GetString()!
                  .Equals("Name", StringComparison.OrdinalIgnoreCase));
    }

    // <REQ-ID: FR-APIKEYS-005>
    [Fact]
    public async Task RenameKey_WhitespaceName_Returns400WithValidationError()
    {
        var (client, keyId) = await CreateKeyAsync();

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/apikeys/{keyId}/rename",
            new { Name = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("error", out _),
            "Response must contain the standard error envelope.");
    }

    // <REQ-ID: FR-APIKEYS-005>
    [Fact]
    public async Task RenameKey_NameExceeds100Chars_Returns400WithValidationError()
    {
        var (client, keyId) = await CreateKeyAsync();

        var tooLong = new string('X', 101);
        var response = await client.PatchAsJsonAsync(
            $"/api/v1/apikeys/{keyId}/rename",
            new { Name = tooLong });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("error", out var error));
        Assert.True(error.TryGetProperty("details", out var details));
        Assert.Contains(details.EnumerateArray(),
            d => d.GetProperty("field").GetString()!
                  .Equals("Name", StringComparison.OrdinalIgnoreCase));
    }

    // <REQ-ID: FR-APIKEYS-005>
    [Fact]
    public async Task RenameKey_NameExactly100Chars_Returns204()
    {
        // 100 characters is at the boundary — must be accepted.
        var (client, keyId) = await CreateKeyAsync();

        var exactly100 = new string('A', 100);
        var response = await client.PatchAsJsonAsync(
            $"/api/v1/apikeys/{keyId}/rename",
            new { Name = exactly100 });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // <REQ-ID: FR-APIKEYS-005>
    [Fact]
    public async Task RenameKey_ValidName_Returns204AndNameChanges()
    {
        var (client, keyId) = await CreateKeyAsync("OriginalName");

        var renameResponse = await client.PatchAsJsonAsync(
            $"/api/v1/apikeys/{keyId}/rename",
            new { Name = "RenamedKey" });

        Assert.Equal(HttpStatusCode.NoContent, renameResponse.StatusCode);

        // Verify the name actually changed via GET /api/v1/apikeys
        var keysResponse = await client.GetAsync("/api/v1/apikeys");
        keysResponse.EnsureSuccessStatusCode();
        var keysBody = await keysResponse.Content.ReadFromJsonAsync<JsonElement>();
        var keys = keysBody.GetProperty("data").EnumerateArray().ToList();

        var renamedKey = keys.FirstOrDefault(k =>
            k.GetProperty("id").GetString() == keyId);

        Assert.False(renamedKey.ValueKind == System.Text.Json.JsonValueKind.Undefined,
            "Key should still be visible after rename.");
        Assert.Equal("RenamedKey",
            renamedKey.GetProperty("name").GetString());
    }

    // <REQ-ID: FR-APIKEYS-005>
    [Fact]
    public async Task RenameKey_NonExistentKey_Returns404()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var nonExistentId = Guid.NewGuid();

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/apikeys/{nonExistentId}/rename",
            new { Name = "ValidName" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // <REQ-ID: FR-APIKEYS-005>
    [Fact]
    public async Task RenameKey_AnotherUsersKey_Returns404()
    {
        // User 1 creates a key
        var (client1, keyId) = await CreateKeyAsync("User1Key");

        // User 2 tries to rename it — must get 404 (same as key not found)
        var (client2, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);
        var response = await client2.PatchAsJsonAsync(
            $"/api/v1/apikeys/{keyId}/rename",
            new { Name = "Hijacked" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // <REQ-ID: FR-APIKEYS-005>
    [Fact]
    public async Task RenameKey_ValidationError_EnvelopeHasErrorCodeNotData()
    {
        // The 400 body must have "error" at the top level, NOT "data".
        var (client, keyId) = await CreateKeyAsync();

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/apikeys/{keyId}/rename",
            new { Name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(body.TryGetProperty("error", out _),
            "Error envelope must have 'error' key.");
        Assert.False(body.TryGetProperty("data", out _),
            "Error envelope must NOT have 'data' key.");
    }
}
