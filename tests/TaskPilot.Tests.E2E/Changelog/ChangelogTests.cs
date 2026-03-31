using System.Text.RegularExpressions;

namespace TaskPilot.Tests.E2E.Changelog;

[Collection("Playwright")]
public class ChangelogTests(PlaywrightFixture fixture)
{
    // Matches rendered version numbers like "v1.0", "v1.2", "v2.0" — NOT "v@..." or "v@(..."
    private static readonly Regex VersionPattern = new(@"v\d+\.\d+");

    [Fact]
    public async Task ChangelogPage_NavLink_IsVisibleInSidebar()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.WaitForSelectorAsync(".tp-changelog-nav-link", new() { Timeout = 10000 });

        var content = await page.ContentAsync();
        Assert.Contains("What's new", content);
        Assert.DoesNotContain("An unhandled error", content);
    }

    [Fact]
    public async Task ChangelogPage_NavLink_ShowsRenderedVersionNumber()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.WaitForSelectorAsync(".tp-version-pill", new() { Timeout = 10000 });

        var pill = await page.QuerySelectorAsync(".tp-version-pill");
        Assert.NotNull(pill);
        var text = await pill.InnerTextAsync();

        // Must match "v1.2" pattern — not literal "v@..." from unrendered Razor
        Assert.Matches(VersionPattern, text);
    }

    [Fact]
    public async Task ChangelogPage_VersionBadges_ShowRenderedVersionNumbers()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/changelog");
        await page.WaitForSelectorAsync(".tp-version-badge", new() { Timeout = 10000 });

        var badges = await page.QuerySelectorAllAsync(".tp-version-badge");
        Assert.True(badges.Count > 0, "Expected at least one version badge");

        foreach (var badge in badges)
        {
            var text = await badge.InnerTextAsync();
            Assert.Matches(VersionPattern, text.Trim());
        }
    }

    [Fact]
    public async Task ChangelogPage_NavigatingToPage_ShowsVersionHistory()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/changelog");
        await page.WaitForSelectorAsync(".tp-changelog", new() { Timeout = 10000 });

        var content = await page.ContentAsync();
        Assert.Contains("Changelog", content);
        // Verify actual version numbers are rendered, not raw Razor expressions
        Assert.Matches(VersionPattern, content);
        Assert.DoesNotContain("@version", content);
        Assert.DoesNotContain("An unhandled error", content);
    }

    [Fact]
    public async Task ChangelogPage_ShowsMajorVersionBadge()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/changelog");
        await page.WaitForSelectorAsync(".tp-changelog-version", new() { Timeout = 10000 });

        var content = await page.ContentAsync();
        Assert.Contains("Major", content);
    }

    [Fact]
    public async Task ChangelogPage_ShowsChangeTypeBadges()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/changelog");
        await page.WaitForSelectorAsync(".tp-change-badge", new() { Timeout = 10000 });

        var badges = await page.QuerySelectorAllAsync(".tp-change-badge");
        Assert.True(badges.Count > 0, "Expected at least one change type badge");
    }

    [Fact]
    public async Task ChangelogPage_NavLink_ClickNavigatesToPage()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.WaitForSelectorAsync(".tp-changelog-nav-link", new() { Timeout = 10000 });
        await page.ClickAsync(".tp-changelog-nav-link");
        await page.WaitForURLAsync("**/changelog", new() { Timeout = 5000 });

        var content = await page.ContentAsync();
        Assert.Contains("What's new", content);
        Assert.DoesNotContain("An unhandled error", content);
    }
}
