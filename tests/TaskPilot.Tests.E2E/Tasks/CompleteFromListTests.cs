namespace TaskPilot.Tests.E2E.Tasks;

[Collection("Playwright")]
public class CompleteFromListTests(PlaywrightFixture fixture)
{
    private static async Task<string> CreateTaskViaModalAsync(Microsoft.Playwright.IPage page)
    {
        await page.GotoAsync("/tasks");
        await page.WaitForSelectorAsync("button:has-text('New Task')", new() { Timeout = 10000 });
        await page.ClickAsync("button:has-text('New Task')");
        await page.WaitForSelectorAsync("#taskModal.show, .modal.show", new() { Timeout = 5000 });

        var taskTitle = $"Close {Guid.NewGuid().ToString("N")[..8]}";
        await page.FillAsync("#taskModal input[name='Title']", taskTitle);
        await page.EvalOnSelectorAsync(
            "#taskModal select[name='TaskTypeId'], #taskModal select[name='taskTypeId']",
            "el => { const opt = Array.from(el.options).find(o => o.value); if (opt) el.value = opt.value; }");
        await page.ClickAsync("#taskModal button[type='submit']");
        await page.WaitForLoadStateAsync();
        return taskTitle;
    }

    [Fact]
    public async Task TaskList_ActiveRow_ShowsCompleteButton()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        var title = await CreateTaskViaModalAsync(page);

        var btn = page.Locator($"button.tp-complete-btn[aria-label=\"Complete task '{title}'\"]");
        Assert.Equal(1, await btn.CountAsync());
    }

    [Fact]
    public async Task TaskList_ClickComplete_RemovesRowFromActiveViewAndShowsToast()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        var title = await CreateTaskViaModalAsync(page);

        await page.ClickAsync($"button.tp-complete-btn[aria-label=\"Complete task '{title}'\"]");
        await page.WaitForLoadStateAsync();
        await page.WaitForTimeoutAsync(500);

        var content = await page.ContentAsync();
        Assert.Contains("completed!", content);
        // Default view is show=active, so the completed task must no longer be listed.
        Assert.DoesNotContain($"Complete task '{title}'", content);
        Assert.DoesNotContain($">{title}<", content.Replace("\n", "").Replace("  ", ""));
    }

    [Fact]
    public async Task TaskList_ClickComplete_PreservesFiltersViaReturnUrl()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        var title = await CreateTaskViaModalAsync(page);

        await page.GotoAsync("/tasks?show=all&sortBy=title&sortDir=asc");
        await page.ClickAsync($"button.tp-complete-btn[aria-label=\"Complete task '{title}'\"]");
        await page.WaitForLoadStateAsync();

        Assert.Contains("show=all", page.Url);
        Assert.Contains("sortBy=title", page.Url);

        // In show=all the task stays listed, now with the static completed marker instead of a button.
        var row = page.Locator("tr", new() { HasText = title });
        Assert.Equal(1, await row.Locator(".tp-complete-done").CountAsync());
        Assert.Equal(0, await row.Locator("button.tp-complete-btn").CountAsync());
    }
}
