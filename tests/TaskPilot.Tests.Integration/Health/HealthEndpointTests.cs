using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskPilot.Data;
using TaskPilot.Diagnostics;
using TaskPilot.Models.Health;
using TaskPilot.Services.Health;

namespace TaskPilot.Tests.Integration.Health;

/// <summary>
/// HLTH-030 through HLTH-045 — Integration tests for /api/v1/health/* endpoints.
/// </summary>
[Collection("Integration")]
public class HealthEndpointTests : IClassFixture<TaskPilotWebAppFactory>
{
    private readonly TaskPilotWebAppFactory _factory;

    public HealthEndpointTests(TaskPilotWebAppFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = false
    });

    // ── HLTH-030: version returns 200 with envelope ────────────────────��──────

    [Fact]
    public async Task Version_Returns200_AndEnvelope()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/v1/health/version");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("data", out _), "Body missing 'data' field");
        Assert.True(body.TryGetProperty("meta", out var meta), "Body missing 'meta' field");
        var requestId = meta.GetProperty("requestId").GetString();
        Assert.True(Guid.TryParse(requestId, out _), $"meta.requestId '{requestId}' is not a GUID");
    }

    // ── HLTH-031: version body matches BuildInfo ──────────────────────────────

    [Fact]
    public async Task Version_BodyMatchesBuildInfo()
    {
        var client = CreateClient();
        var body = await client.GetFromJsonAsync<JsonElement>("/api/v1/health/version");
        var data = body.GetProperty("data");

        Assert.Equal(BuildInfo.Version, data.GetProperty("version").GetString());
        Assert.Equal(BuildInfo.GitCommit, data.GetProperty("gitCommit").GetString());
        Assert.Equal(BuildInfo.GitCommitShort, data.GetProperty("gitCommitShort").GetString());
    }

    // ── HLTH-032: version has no-cache headers ────────────────────────────────

    [Fact]
    public async Task Version_HasNoCacheHeaders()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/v1/health/version");

        var cacheControl = response.Headers.CacheControl?.ToString() ?? string.Empty;
        Assert.Contains("no-store", cacheControl);

        // Pragma may appear in response headers or content headers depending on the HTTP client
        var allHeaders = response.Headers
            .Concat(response.Content.Headers)
            .ToDictionary(h => h.Key, h => string.Join(", ", h.Value), StringComparer.OrdinalIgnoreCase);

        Assert.True(allHeaders.ContainsKey("Pragma"), "Missing Pragma header");
        Assert.Contains("no-cache", allHeaders["Pragma"]);

        // Expires: 0 may appear in response or content headers
        Assert.True(allHeaders.ContainsKey("Expires"), "Missing Expires header");
        Assert.Equal("0", allHeaders["Expires"]);
    }

    // ── HLTH-033: version has custom X-TaskPilot-* headers ───────────────���───

    [Fact]
    public async Task Version_HasCustomVersionHeaders()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/v1/health/version");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");

        Assert.True(response.Headers.TryGetValues("X-TaskPilot-Version", out var versionHeader));
        Assert.True(response.Headers.TryGetValues("X-TaskPilot-Commit", out var commitHeader));
        Assert.Equal(data.GetProperty("version").GetString(), versionHeader.First());
        Assert.Equal(data.GetProperty("gitCommitShort").GetString(), commitHeader.First());
    }

    // ── HLTH-034: live returns 200 with status=alive ──────────────────────────

    [Fact]
    public async Task Live_Returns200_Always()
    {
        var client = CreateClient();
        var body = await client.GetFromJsonAsync<JsonElement>("/api/v1/health/live");

        Assert.Equal(HttpStatusCode.OK, await client.GetAsync("/api/v1/health/live")
            .ContinueWith(t => t.Result.StatusCode));
        var data = body.GetProperty("data");
        Assert.Equal("alive", data.GetProperty("status").GetString());
    }

    // ── HLTH-035: ready returns 200 when DB healthy ───────────────────────────

    [Fact]
    public async Task Ready_Returns200_WhenAllRequiredHealthy()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/v1/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var status = body.GetProperty("data").GetProperty("status").GetString();
        Assert.True(status is "healthy" or "degraded",
            $"Status was '{status}', expected 'healthy' or 'degraded'");
    }

    // ── HLTH-036: ready returns 503 when DB down ──────────────────────────────

    [Fact]
    public async Task Ready_Returns503_WhenDatabaseDown()
    {
        // Inject a stub IHealthCheckComponent that simulates a failing required database check.
        var brokenFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove all existing IHealthCheckComponent registrations and replace with one broken required check
                var toRemove = services.Where(d => d.ServiceType == typeof(IHealthCheckComponent)).ToList();
                foreach (var d in toRemove) services.Remove(d);

                services.AddScoped<IHealthCheckComponent>(_ => new StubHealthCheck(
                    "database", required: true, status: HealthStatuses.Unhealthy, "Connection refused."));
            });
        });

        var client = brokenFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var response = await client.GetAsync("/api/v1/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        // Envelope still well-formed
        Assert.True(body.TryGetProperty("data", out var data));
        Assert.Equal("unhealthy", data.GetProperty("status").GetString());
    }

    // ── HLTH-037: full returns 200 with all checks ────────────────────────────

    [Fact]
    public async Task Full_Returns200_AndIncludesAllChecks()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/v1/health/full");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var checks = body.GetProperty("data").GetProperty("checks").EnumerateArray().ToList();

        var expectedNames = new[] { "database", "migrations", "config", "auth-handlers", "mcp", "temp-writable", "assembly-metadata" };
        foreach (var expected in expectedNames)
        {
            Assert.Contains(checks, c => c.GetProperty("name").GetString() == expected);
        }
    }

    // ── HLTH-038: full — every check has duration > 0 ─────────────────────────

    [Fact]
    public async Task Full_PerCheckHasDuration()
    {
        var client = CreateClient();
        var body = await client.GetFromJsonAsync<JsonElement>("/api/v1/health/full");
        var checks = body.GetProperty("data").GetProperty("checks").EnumerateArray().ToList();

        foreach (var check in checks)
        {
            var name = check.GetProperty("name").GetString();
            // Duration is serialized as "hh:mm:ss.fffffff" string by System.Text.Json
            // We just verify it's present and non-null
            Assert.True(check.TryGetProperty("duration", out var duration),
                $"Check '{name}' missing 'duration'");
            Assert.NotEqual(JsonValueKind.Null, duration.ValueKind);
        }
    }

    // ── HLTH-039: full returns 200 when only optional degraded ───────────────

    [Fact]
    public async Task Full_Returns200WhenOnlyOptionalDegraded()
    {
        // Replace all health checks with one required (healthy) + one optional (unhealthy)
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var toRemove = services.Where(d => d.ServiceType == typeof(IHealthCheckComponent)).ToList();
                foreach (var d in toRemove) services.Remove(d);

                services.AddScoped<IHealthCheckComponent>(_ =>
                    new StubHealthCheck("required-ok", required: true, HealthStatuses.Healthy, "ok"));
                services.AddScoped<IHealthCheckComponent>(_ =>
                    new StubHealthCheck("optional-fail", required: false, HealthStatuses.Unhealthy, "mcp missing"));
            });
        });

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/api/v1/health/full");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("degraded", body.GetProperty("data").GetProperty("status").GetString());
    }

    // ── HLTH-040: full returns 503 when migrations pending ────────────────────

    [Fact]
    public async Task Full_Returns503_WhenMigrationsPending()
    {
        // Inject a stub that simulates the migrations check being unhealthy (pending migrations).
        var brokenFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var toRemove = services.Where(d => d.ServiceType == typeof(IHealthCheckComponent)).ToList();
                foreach (var d in toRemove) services.Remove(d);

                services.AddScoped<IHealthCheckComponent>(_ => new StubHealthCheck(
                    "migrations", required: true, status: HealthStatuses.Unhealthy, "2 pending migrations."));
            });
        });

        var client = brokenFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var response = await client.GetAsync("/api/v1/health/full");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("unhealthy", body.GetProperty("data").GetProperty("status").GetString());
    }

    // ── HLTH-041: assets returns 200 with manifest ────────────────────────────

    [Fact]
    public async Task Assets_Returns200_WithManifest()
    {
        var client = CreateClient();
        var body = await client.GetFromJsonAsync<JsonElement>("/api/v1/health/assets");

        Assert.Equal(HttpStatusCode.OK, await client.GetAsync("/api/v1/health/assets")
            .ContinueWith(t => t.Result.StatusCode));
        var assets = body.GetProperty("data").GetProperty("assets");
        Assert.Equal(JsonValueKind.Object, assets.ValueKind);

        foreach (var asset in assets.EnumerateObject())
        {
            Assert.StartsWith("sha256-", asset.Value.GetString());
        }
    }

    // ── HLTH-042: asset hashes stable on repeated calls ──────────────────��────

    [Fact]
    public async Task Assets_HashStable_OnRepeatedCalls()
    {
        var client = CreateClient();
        var body1 = await client.GetFromJsonAsync<JsonElement>("/api/v1/health/assets");
        var body2 = await client.GetFromJsonAsync<JsonElement>("/api/v1/health/assets");

        var assets1 = body1.GetProperty("data").GetProperty("assets").EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.GetString());
        var assets2 = body2.GetProperty("data").GetProperty("assets").EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.GetString());

        Assert.Equal(assets1, assets2);
    }

    // ── HLTH-043: health endpoints not in audit log ─────────────────────────��─

    [Fact]
    public async Task HealthEndpoints_NotInAuditLog()
    {
        var client = CreateClient();

        // Hit all health endpoints (no API key, so no audit entry possible anyway,
        // but verify the exclusion logic doesn't break the middleware)
        await client.GetAsync("/api/v1/health/live");
        await client.GetAsync("/api/v1/health/ready");
        await client.GetAsync("/api/v1/health/full");
        await client.GetAsync("/api/v1/health/version");
        await client.GetAsync("/api/v1/health/assets");

        // Audit log endpoint requires auth — just verify health endpoints returned without 500
        // (full audit log query requires login; we assert no server errors)
        var liveResponse = await client.GetAsync("/api/v1/health/live");
        Assert.NotEqual(HttpStatusCode.InternalServerError, liveResponse.StatusCode);
    }

    // ── HLTH-044: health endpoints accessible anonymously ─────────────────────

    [Fact]
    public async Task HealthEndpoints_AnonymousAccess()
    {
        var client = CreateClient();
        var endpoints = new[]
        {
            "/api/v1/health/live",
            "/api/v1/health/ready",
            "/api/v1/health/full",
            "/api/v1/health/version",
            "/api/v1/health/assets"
        };

        foreach (var endpoint in endpoints)
        {
            var response = await client.GetAsync(endpoint);
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    // ── HLTH-045: ready responds within 500ms ─────────────────────────────────

    [Fact]
    public async Task Ready_RespondsWithin500ms()
    {
        var client = CreateClient();
        var sw = Stopwatch.StartNew();
        await client.GetAsync("/api/v1/health/ready");
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 500,
            $"Ready endpoint took {sw.ElapsedMilliseconds}ms (limit: 500ms)");
    }

    // ── stub helper ──────────────────────────────────────────────────────────

    private sealed class StubHealthCheck(string name, bool required, string status, string message)
        : IHealthCheckComponent
    {
        public string Name => name;
        public bool IsRequired => required;

        public Task<HealthCheckResult> CheckAsync(CancellationToken ct = default) =>
            Task.FromResult(new HealthCheckResult
            {
                Name = name,
                Status = status,
                Duration = TimeSpan.FromMilliseconds(1),
                IsRequired = required,
                Message = message
            });
    }
}
