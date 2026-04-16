using System.Net.Http.Json;
using System.Text.Json;

namespace TaskPilot.Tests.Integration.Smoke;

/// <summary>
/// HLTH-060 through HLTH-065 — Smoke tests parameterized by SMOKE_BASE_URL environment variable.
/// These tests require the application to be running at the target URL.
/// Default: http://localhost:5125.
///
/// Usage:
///   dotnet test --filter "Category=Smoke"
///   SMOKE_BASE_URL=https://taskpilot.azurewebsites.net dotnet test --filter "Category=Smoke"
///   EXPECTED_COMMIT=a1b2c3d dotnet test --filter "Category=Smoke"
/// </summary>
[Trait("Category", "Smoke")]
public class DeploymentSmokeTests
{
    private static readonly string BaseUrl =
        System.Environment.GetEnvironmentVariable("SMOKE_BASE_URL") ?? "http://localhost:5125";

    private static readonly string? ExpectedCommit =
        System.Environment.GetEnvironmentVariable("EXPECTED_COMMIT");

    private static HttpClient CreateClient() => new(new HttpClientHandler
    {
        AllowAutoRedirect = false
    })
    {
        BaseAddress = new Uri(BaseUrl),
        Timeout = TimeSpan.FromSeconds(30)
    };

    // ── HLTH-060: version endpoint reachable ─────────────────────────────────

    [Fact]
    public async Task Smoke_VersionEndpointReachable()
    {
        using var client = CreateClient();
        var body = await client.GetFromJsonAsync<JsonElement>("/api/v1/health/version");

        Assert.True(body.TryGetProperty("data", out var data), "Missing 'data' in response");
        var version = data.GetProperty("version").GetString();
        Assert.False(string.IsNullOrWhiteSpace(version), "Version is empty");
        Assert.Matches(@"^\d+\.\d+\.\d+", version!);
    }

    // ── HLTH-061: deployed commit matches expected ────────────────────────────

    [Fact]
    public async Task Smoke_DeployedCommitMatchesExpected()
    {
        if (string.IsNullOrWhiteSpace(ExpectedCommit))
        {
            // Skip gracefully when EXPECTED_COMMIT not provided
            return;
        }

        using var client = CreateClient();
        var body = await client.GetFromJsonAsync<JsonElement>("/api/v1/health/version");
        var deployedShort = body.GetProperty("data").GetProperty("gitCommitShort").GetString() ?? "";
        var expectedShort = ExpectedCommit[..Math.Min(7, ExpectedCommit.Length)].ToLowerInvariant();

        Assert.Equal(expectedShort, deployedShort.ToLowerInvariant());
    }

    // ── HLTH-062: full health is green ───────────────────────────────────────

    [Fact]
    public async Task Smoke_FullHealthGreen()
    {
        using var client = CreateClient();
        var response = await client.GetAsync("/api/v1/health/full");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var status = body.GetProperty("data").GetProperty("status").GetString();
        Assert.Equal("healthy", status);
    }

    // ── HLTH-063: no CDN caching ──────────────────────────────────────────────

    [Fact]
    public async Task Smoke_NoCdnCachingDetected()
    {
        using var client = CreateClient();

        var r1 = await client.GetAsync($"/api/v1/health/version?_={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
        var r2 = await client.GetAsync($"/api/v1/health/version?_={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 1}");

        Assert.False(r1.Headers.Contains("Age"), "Response 1 has Age header — CDN may be caching");
        Assert.False(r2.Headers.Contains("Age"), "Response 2 has Age header — CDN may be caching");

        var body1 = await r1.Content.ReadFromJsonAsync<JsonElement>();
        var body2 = await r2.Content.ReadFromJsonAsync<JsonElement>();
        var sha1 = body1.GetProperty("data").GetProperty("gitCommitShort").GetString();
        var sha2 = body2.GetProperty("data").GetProperty("gitCommitShort").GetString();
        Assert.Equal(sha1, sha2);
    }

    // ── HLTH-064: asset manifest hashes match served assets ──────────────────

    [Fact]
    public async Task Smoke_AssetManifestMatchesServedAssets()
    {
        using var client = CreateClient();
        var manifest = await client.GetFromJsonAsync<JsonElement>("/api/v1/health/assets");
        var assets = manifest.GetProperty("data").GetProperty("assets");

        foreach (var asset in assets.EnumerateObject())
        {
            var assetPath = asset.Name;
            var expectedHash = asset.Value.GetString()!;

            var assetBytes = await client.GetByteArrayAsync(assetPath);
            var computedHash = "sha256-" + Convert.ToBase64String(
                System.Security.Cryptography.SHA256.HashData(assetBytes));

            Assert.Equal(expectedHash, computedHash);
        }
    }

    // ── HLTH-065: runs against local and Azure ────────────────────────────────

    [Fact]
    public async Task Smoke_RunsAgainstLocalAndAzure()
    {
        // This test documents the intent that the same suite runs against both environments.
        // In CI: run with SMOKE_BASE_URL=http://localhost:5125, then again with
        //        SMOKE_BASE_URL=https://taskpilot.azurewebsites.net
        // Here we simply verify the version endpoint at the configured BaseUrl.
        using var client = CreateClient();
        var response = await client.GetAsync("/api/v1/health/version");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
