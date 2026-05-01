namespace TaskPilot.Tests.E2E.Dashboard;

[Collection("Playwright")]
public class DashboardTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Dashboard_Loads_WithoutErrors()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.WaitForSelectorAsync("text=Dashboard", new() { Timeout = 10000 });
        Assert.DoesNotContain("An unhandled error", await page.ContentAsync());
    }

    [Fact]
    public async Task Dashboard_ShowsFiveSummaryCards()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.WaitForSelectorAsync(".tp-stats-grid", new() { Timeout = 10000 });
        var content = await page.ContentAsync();

        Assert.Contains("Total Active", content);
        Assert.Contains("Completed Today", content);
        Assert.Contains("Overdue", content);
        Assert.Contains("In Progress", content);
        Assert.Contains("Blocked", content);
    }

    [Fact]
    public async Task Dashboard_ChartsSection_Renders()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.WaitForSelectorAsync(".tp-charts-grid", new() { Timeout = 10000 });
        var content = await page.ContentAsync();

        Assert.True(content.Contains("Completed per Week") || content.Contains("By Priority"),
            "Expected chart section titles");
    }

    [Fact]
    public async Task Dashboard_Navigation_TasksLinkWorks()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.WaitForSelectorAsync("a[href='/tasks']", new() { Timeout = 10000 });
        await page.ClickAsync("a[href='/tasks']");
        await page.WaitForURLAsync("**/tasks", new() { Timeout = 10000 });
        Assert.Contains("/tasks", page.Url);
    }

    [Fact]
    public async Task Dashboard_Navigation_AuditLinkWorks()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.WaitForSelectorAsync("a[href='/audit']", new() { Timeout = 10000 });
        await page.ClickAsync("a[href='/audit']");
        await page.WaitForURLAsync("**/audit", new() { Timeout = 10000 });
        Assert.Contains("/audit", page.Url);
    }

    [Fact]
    public async Task Dashboard_IncompleteCard_NavigatesToFilteredTasksView()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        // The card shows the empty-state copy for a fresh user. Quick-add a task so the
        // sub-tiles render with a count.
        await page.WaitForSelectorAsync("form.tp-quick-add input[name='title']", new() { Timeout = 10000 });
        await page.FillAsync("form.tp-quick-add input[name='title']", "needs doing");
        await page.ClickAsync("form.tp-quick-add button[type='submit']");
        await page.WaitForURLAsync("**/", new() { Timeout = 10000 });

        var tile = await page.WaitForSelectorAsync(".tp-incomplete-tile-not-started",
            new() { Timeout = 10000 });
        Assert.NotNull(tile);
        await tile!.ClickAsync();
        await page.WaitForURLAsync("**/tasks?view=incomplete&status=NotStarted", new() { Timeout = 10000 });
        Assert.Contains("view=incomplete", page.Url);
        Assert.Contains("status=NotStarted", page.Url);
    }

    [Fact]
    public async Task Dashboard_OverdueCard_NavigatesToOverdueIncompleteView()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.WaitForSelectorAsync(".tp-stat-card-link", new() { Timeout = 10000 });
        await page.ClickAsync(".tp-stat-card-link");
        await page.WaitForURLAsync("**/tasks?view=incomplete&overdue=true", new() { Timeout = 10000 });
        Assert.Contains("view=incomplete", page.Url);
        Assert.Contains("overdue=true", page.Url);
    }
}
