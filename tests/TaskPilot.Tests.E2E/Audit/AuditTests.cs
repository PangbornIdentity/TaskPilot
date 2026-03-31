namespace TaskPilot.Tests.E2E.Audit;

[Collection("Playwright")]
public class AuditTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task AuditPage_DefaultTab_ShowsTaskHistory()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/audit");
        await page.WaitForSelectorAsync(".nav-tabs", new() { Timeout = 10000 });

        var content = await page.ContentAsync();
        Assert.Contains("Task History", content);
        Assert.Contains("API Access", content);
        Assert.DoesNotContain("An unhandled error", content);
    }

    [Fact]
    public async Task AuditPage_TaskHistoryTab_EmptyState_ShowsNoHistoryMessage()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/audit?tab=tasks");
        await page.WaitForSelectorAsync(".tp-card", new() { Timeout = 10000 });

        var content = await page.ContentAsync();
        Assert.True(
            content.Contains("No task history") || content.Contains("Changes to tasks"),
            "Expected empty state for new user on Task History tab");
    }

    [Fact]
    public async Task AuditPage_ApiAccessTab_ShowsSummaryCards()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/audit?tab=api");
        await page.WaitForSelectorAsync(".tp-stats-grid", new() { Timeout = 10000 });

        var content = await page.ContentAsync();
        Assert.Contains("Total Requests", content);
        Assert.Contains("Active API Keys", content);
        Assert.DoesNotContain("An unhandled error", content);
    }

    [Fact]
    public async Task AuditPage_ApiAccessTab_EmptyState_ShowsNoLogsMessage()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/audit?tab=api");
        await page.WaitForSelectorAsync(".tp-card", new() { Timeout = 10000 });

        var content = await page.ContentAsync();
        Assert.True(
            content.Contains("No audit") || content.Contains("API key activity"),
            "Expected empty state for new user on API Access tab");
    }

    [Fact]
    public async Task AuditPage_TaskHistoryTab_AfterTaskEdit_ShowsLog()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        // Create a task
        await page.GotoAsync("/tasks");
        await page.WaitForSelectorAsync("button:has-text('New Task')", new() { Timeout = 10000 });
        await page.ClickAsync("button:has-text('New Task')");
        await page.WaitForSelectorAsync("#taskModal.show", new() { Timeout = 5000 });

        var taskTitle = $"Audit Log Test {Guid.NewGuid().ToString("N")[..6]}";
        await page.FillAsync("#taskModal input[name='Title']", taskTitle);
        await page.ClickAsync("#taskModal button[type='submit']");
        await page.WaitForLoadStateAsync();
        await page.WaitForTimeoutAsync(500);

        // Navigate to detail and edit
        var taskLink = await page.QuerySelectorAsync($"a:has-text('{taskTitle}')");
        Assert.NotNull(taskLink);
        await taskLink.ClickAsync();
        await page.WaitForURLAsync("**/tasks/**", new() { Timeout = 5000 });

        var updatedTitle = $"Audit Edited {Guid.NewGuid().ToString("N")[..6]}";
        await page.FillAsync("input[name='Title']", updatedTitle);
        await page.ClickAsync("button:has-text('Save Changes')");
        await page.WaitForLoadStateAsync();
        await page.WaitForTimeoutAsync(500);

        // Navigate to audit task history tab and verify logs appear
        await page.GotoAsync("/audit?tab=tasks");
        await page.WaitForSelectorAsync(".tp-card", new() { Timeout = 10000 });

        var content = await page.ContentAsync();
        Assert.False(
            content.Contains("No task history") && !content.Contains("<tbody"),
            "Expected task history entries after editing a task");
        Assert.DoesNotContain("An unhandled error", content);
    }

    [Fact]
    public async Task AuditPage_TabSwitch_NavigatesToApiTab()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/audit?tab=tasks");
        await page.WaitForSelectorAsync(".nav-tabs", new() { Timeout = 10000 });

        await page.ClickAsync("a[href='/audit?tab=api']");
        await page.WaitForSelectorAsync(".tp-stats-grid", new() { Timeout = 10000 });

        var content = await page.ContentAsync();
        Assert.Contains("Total Requests", content);
        Assert.DoesNotContain("An unhandled error", content);
    }
}
