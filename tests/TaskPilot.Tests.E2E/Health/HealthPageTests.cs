using System.Text.Json;
using Microsoft.Playwright;

namespace TaskPilot.Tests.E2E.Health;

/// <summary>
/// HLTH-050 through HLTH-057 — E2E tests for the public /health page and sidebar version pill.
/// Requires the application to be running at http://localhost:5125.
/// </summary>
[Collection("Playwright")]
public class HealthPageTests(PlaywrightFixture fixture)
{
    // ── HLTH-050: /health loads anonymously ──────────────────────────────────

    [Fact]
    public async Task HealthPage_LoadsAnonymously()
    {
        var context = await fixture.NewContextAsync();
        await using var _ = context;
        var page = await context.NewPageAsync();

        var response = await page.GotoAsync("/health");

        // Should not redirect to login
        Assert.NotNull(response);
        Assert.Equal(200, response.Status);
        Assert.DoesNotContain("/auth/login", page.Url);
    }

    // ── HLTH-051: /health shows HEALTHY badge ────────────────────────────────

    [Fact]
    public async Task HealthPage_ShowsHealthyBadge()
    {
        var context = await fixture.NewContextAsync();
        await using var _ = context;
        var page = await context.NewPageAsync();

        await page.GotoAsync("/health");
        await page.WaitForSelectorAsync(".tp-health-status", new() { Timeout = 10000 });

        var statusText = await page.TextContentAsync(".tp-health-status");
        Assert.NotNull(statusText);

        var apiResponse = await page.APIRequest.GetAsync($"{PlaywrightFixture.BaseUrl}/api/v1/health/full");
        var body = JsonDocument.Parse(await apiResponse.TextAsync());
        var apiStatus = body.RootElement.GetProperty("data").GetProperty("status").GetString();

        Assert.Equal(apiStatus?.ToUpperInvariant(), statusText.Trim());
    }

    // ── HLTH-052: page version matches API version ────────────────────────────

    [Fact]
    public async Task HealthPage_VersionMatchesApi()
    {
        var context = await fixture.NewContextAsync();
        await using var _ = context;
        var page = await context.NewPageAsync();

        await page.GotoAsync("/health");
        await page.WaitForSelectorAsync(".tp-health-version", new() { Timeout = 10000 });

        var pageVersion = await page.TextContentAsync(".tp-health-version");

        // Also get from API
        var apiResponse = await page.APIRequest.GetAsync($"{PlaywrightFixture.BaseUrl}/api/v1/health/version");
        var body = JsonDocument.Parse(await apiResponse.TextAsync());
        var apiVersion = body.RootElement.GetProperty("data").GetProperty("version").GetString();

        Assert.Equal(apiVersion?.Trim(), pageVersion?.Trim());
    }

    // ── HLTH-053: page commit matches API commit ──────────────────────────────

    [Fact]
    public async Task HealthPage_CommitMatchesApi()
    {
        var context = await fixture.NewContextAsync();
        await using var _ = context;
        var page = await context.NewPageAsync();

        await page.GotoAsync("/health");
        await page.WaitForSelectorAsync(".tp-health-commit", new() { Timeout = 10000 });

        var pageCommit = await page.TextContentAsync(".tp-health-commit");

        var apiResponse = await page.APIRequest.GetAsync($"{PlaywrightFixture.BaseUrl}/api/v1/health/version");
        var body = JsonDocument.Parse(await apiResponse.TextAsync());
        var apiCommitShort = body.RootElement.GetProperty("data").GetProperty("gitCommitShort").GetString();

        Assert.Equal(apiCommitShort?.Trim(), pageCommit?.Trim());
    }

    // ── HLTH-054: per-check rows rendered ────────────────────────────────────

    [Fact]
    public async Task HealthPage_PerCheckRowsRendered()
    {
        var context = await fixture.NewContextAsync();
        await using var _ = context;
        var page = await context.NewPageAsync();

        await page.GotoAsync("/health");
        await page.WaitForSelectorAsync("table tbody tr", new() { Timeout = 10000 });

        var rows = await page.QuerySelectorAllAsync("table tbody tr");
        Assert.True(rows.Count >= 7, $"Expected at least 7 check rows, found {rows.Count}");

        // Each row should have name, status, duration
        foreach (var row in rows)
        {
            var cells = await row.QuerySelectorAllAsync("td");
            Assert.True(cells.Count >= 3, "Check row has fewer than 3 cells");
            var name = await cells[0].TextContentAsync();
            Assert.False(string.IsNullOrWhiteSpace(name), "Check row name is empty");
        }
    }

    // ── HLTH-055: Raw JSON link navigates to /full endpoint ──────────────────

    [Fact]
    public async Task HealthPage_RawJsonLink_GoesToFullEndpoint()
    {
        var context = await fixture.NewContextAsync();
        await using var _ = context;
        var page = await context.NewPageAsync();

        await page.GotoAsync("/health");
        await page.WaitForSelectorAsync(".tp-raw-json-link", new() { Timeout = 10000 });

        // Click and wait for navigation
        var navigationTask = page.WaitForURLAsync("**/api/v1/health/full", new() { Timeout = 10000 });
        await page.ClickAsync(".tp-raw-json-link");
        await navigationTask;

        Assert.Contains("/api/v1/health/full", page.Url);
    }

    // ── HLTH-056: sidebar version pill links to /health ──────────────────────

    [Fact]
    public async Task SidebarVersionPill_LinksToHealthPage()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.WaitForSelectorAsync(".tp-version-pill-link", new() { Timeout = 10000 });
        var href = await page.GetAttributeAsync(".tp-version-pill-link", "href");

        Assert.Equal("/health", href);
    }

    // ── HLTH-057: sidebar version pill text matches API version ──────────────

    [Fact]
    public async Task SidebarVersionPill_TextMatchesApiVersion()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.WaitForSelectorAsync(".tp-version-pill", new() { Timeout = 10000 });
        var pillText = await page.TextContentAsync(".tp-version-pill");

        var apiResponse = await page.APIRequest.GetAsync($"{PlaywrightFixture.BaseUrl}/api/v1/health/version");
        var body = JsonDocument.Parse(await apiResponse.TextAsync());
        var data = body.RootElement.GetProperty("data");
        var apiVersion = data.GetProperty("version").GetString();
        var apiCommitShort = data.GetProperty("gitCommitShort").GetString();

        Assert.NotNull(pillText);
        Assert.Contains(apiVersion!, pillText);
        Assert.Contains(apiCommitShort!, pillText);
    }
}
