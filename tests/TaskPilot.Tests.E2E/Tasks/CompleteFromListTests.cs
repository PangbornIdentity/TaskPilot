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
    public async Task TaskList_ActiveRow_CompleteIsLastActionAfterCloneAndEdit()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        var title = await CreateTaskViaModalAsync(page);

        var row = page.Locator("tr", new() { HasText = title });
        var actions = row.Locator("td.text-end .d-flex > *");
        Assert.Equal(3, await actions.CountAsync());
        Assert.True(await actions.Nth(0).EvaluateAsync<bool>("el => el.classList.contains('tp-clone-btn')"));
        Assert.True(await actions.Nth(1).EvaluateAsync<bool>("el => el.tagName === 'A' && el.classList.contains('btn')"));
        Assert.Equal(1, await actions.Nth(2).Locator("button.tp-complete-btn.btn-outline-success").CountAsync());
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

    // Regression: the third action button must not push the row actions past the card edge on tablets.
    // The card is overflow:hidden, so document-overflow checks cannot catch clipped controls.
    [Theory]
    [InlineData(768)]
    [InlineData(800)]
    [InlineData(1024)]
    public async Task TaskList_TabletViewport_RowActionsFitInsideCard(int viewportWidth)
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        // Worst-case row: long title, In Progress, High priority, overdue (date + Overdue badge).
        var title = $"Prepare quarterly presentation {Guid.NewGuid().ToString("N")[..6]}";
        var taskId = await page.EvaluateAsync<string>(@"async (title) => {
            const r = await fetch('/api/v1/tasks', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ title, taskTypeId: 1, area: 1, priority: 2, status: 1,
                    targetDateType: 0, targetDate: '2020-01-15T00:00:00Z', isRecurring: false })
            });
            const body = await r.json();
            return body.data.id;
        }", title);

        await page.SetViewportSizeAsync(viewportWidth, 1024);
        await page.GotoAsync("/tasks");
        await page.WaitForSelectorAsync(".tp-table", new() { Timeout = 10000 });

        var card = page.Locator(".tp-card:has(.tp-table)");
        var row = page.Locator("tr", new() { HasText = title });
        var cloneBtn = row.Locator("button.tp-clone-btn");
        var completeBtn = row.Locator("button.tp-complete-btn");

        var cardBox = await card.BoundingBoxAsync();
        var cloneBox = await cloneBtn.BoundingBoxAsync();
        var completeBox = await completeBtn.BoundingBoxAsync();
        Assert.NotNull(cardBox);
        Assert.NotNull(cloneBox);
        Assert.NotNull(completeBox);

        // Complete is the last (right-most) action, so its right edge is the one that clips first.
        Assert.True(completeBox!.X + completeBox.Width <= cardBox!.X + cardBox.Width + 0.5,
            $"Complete button right edge {completeBox.X + completeBox.Width} exceeds card right edge {cardBox.X + cardBox.Width} at {viewportWidth}px");
        Assert.True(cloneBox!.X >= cardBox.X - 0.5,
            $"Clone button left edge {cloneBox.X} is before card left edge {cardBox.X} at {viewportWidth}px");

        var overflow = await card.EvaluateAsync<int>("el => el.scrollWidth - el.clientWidth");
        Assert.True(overflow <= 0, $"Table overflows its card by {overflow}px at {viewportWidth}px");
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

        // In show=all the task stays listed with a Completed badge; the complete button is gone
        // but clone and edit remain.
        var row = page.Locator("tr", new() { HasText = title });
        Assert.Equal(1, await row.Locator(".tp-badge-completed").CountAsync());
        Assert.Equal(0, await row.Locator("button.tp-complete-btn").CountAsync());
        Assert.Equal(1, await row.Locator("button.tp-clone-btn").CountAsync());
    }
}
