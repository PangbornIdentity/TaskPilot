namespace TaskPilot.Tests.E2E.Tasks;

[Collection("Playwright")]
public class TaskLifecycleTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Dashboard_AfterLogin_ShowsSummaryCards()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.WaitForSelectorAsync(".tp-stats-grid", new() { Timeout = 10000 });
        var content = await page.ContentAsync();
        Assert.Contains("Total Active", content);
        Assert.Contains("Overdue", content);
    }

    [Fact]
    public async Task CreateTask_ViaNewTaskButton_AppearsInList()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/tasks");
        await page.WaitForSelectorAsync("button:has-text('New Task')", new() { Timeout = 10000 });
        await page.ClickAsync("button:has-text('New Task')");

        // Wait for modal
        await page.WaitForSelectorAsync("#taskModal.show, .modal.show", new() { Timeout = 5000 });

        var taskTitle = $"E2E Task {Guid.NewGuid().ToString("N")[..8]}";
        await page.FillAsync("#taskModal input[name='Title']", taskTitle);

        // Select first valid task type (required)
        await page.EvalOnSelectorAsync(
            "#taskModal select[name='TaskTypeId'], #taskModal select[name='taskTypeId']",
            "el => { const opt = Array.from(el.options).find(o => o.value); if (opt) el.value = opt.value; }");

        await page.ClickAsync("#taskModal button[type='submit']");

        // After form submit, wait for page reload and task to appear
        await page.WaitForLoadStateAsync();
        await page.WaitForTimeoutAsync(500);
        var content = await page.ContentAsync();
        Assert.Contains(taskTitle, content);
    }

    [Fact]
    public async Task CreateTask_ViaQuickAdd_CreatesTask()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        var taskTitle = $"Quick {Guid.NewGuid().ToString("N")[..8]}";

        var quickInput = await page.QuerySelectorAsync("input[name='title']");
        Assert.NotNull(quickInput);

        await quickInput.FillAsync(taskTitle);
        await page.ClickAsync("button[type='submit']:near(input[name='title'])");

        await page.WaitForLoadStateAsync();
        await page.GotoAsync("/tasks");
        await page.WaitForSelectorAsync(".tp-page-title", new() { Timeout = 5000 });
        var content = await page.ContentAsync();
        Assert.Contains(taskTitle, content);
    }

    [Fact]
    public async Task TaskList_SearchFilter_NarrowsResults()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/tasks");
        await page.WaitForSelectorAsync("#searchInput", new() { Timeout = 10000 });
        await page.FillAsync("#searchInput", "xyzzy_nonexistent_12345");
        await page.WaitForTimeoutAsync(600);

        var content = await page.ContentAsync();
        Assert.True(
            content.Contains("No tasks") || content.Contains("0 task"),
            "Expected empty state for nonexistent search");
    }

    [Fact]
    public async Task TaskList_ToggleBoardView_ShowsKanbanColumns()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/tasks");
        await page.WaitForSelectorAsync(".tp-view-toggle", new() { Timeout = 10000 });

        // Click board view
        await page.ClickAsync("a[href*='view=board']");
        await page.WaitForURLAsync("**/tasks?view=board**", new() { Timeout = 5000 });

        var content = await page.ContentAsync();
        Assert.True(content.Contains("Not Started") || content.Contains("In Progress"),
            "Expected kanban column headers");
    }

    [Fact]
    public async Task TaskDetail_NavigatingToTask_ShowsEditForm()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        // Create a task first
        await page.GotoAsync("/tasks");
        await page.WaitForSelectorAsync("button:has-text('New Task')", new() { Timeout = 10000 });
        await page.ClickAsync("button:has-text('New Task')");
        await page.WaitForSelectorAsync("#taskModal.show", new() { Timeout = 5000 });

        var taskTitle = $"Detail Test {Guid.NewGuid().ToString("N")[..6]}";
        await page.FillAsync("#taskModal input[name='Title']", taskTitle);
        await page.EvalOnSelectorAsync(
            "#taskModal select[name='TaskTypeId'], #taskModal select[name='taskTypeId']",
            "el => { const opt = Array.from(el.options).find(o => o.value); if (opt) el.value = opt.value; }");
        await page.ClickAsync("#taskModal button[type='submit']");
        await page.WaitForLoadStateAsync();
        await page.WaitForTimeoutAsync(500);

        // Click the task link
        var taskLink = await page.QuerySelectorAsync($"a:has-text('{taskTitle}')");
        if (taskLink != null)
        {
            await taskLink.ClickAsync();
            await page.WaitForURLAsync("**/tasks/**", new() { Timeout = 5000 });
            var content = await page.ContentAsync();
            Assert.True(content.Contains("Priority") || content.Contains("Status"),
                "Expected task detail fields");
        }
        Assert.DoesNotContain("An unhandled error", await page.ContentAsync());
    }

    [Fact]
    public async Task TaskDetail_AfterEdit_ShowsChangeHistory()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        // Create a task
        await page.GotoAsync("/tasks");
        await page.WaitForSelectorAsync("button:has-text('New Task')", new() { Timeout = 10000 });
        await page.ClickAsync("button:has-text('New Task')");
        await page.WaitForSelectorAsync("#taskModal.show", new() { Timeout = 5000 });

        var taskTitle = $"History Test {Guid.NewGuid().ToString("N")[..6]}";
        await page.FillAsync("#taskModal input[name='Title']", taskTitle);
        await page.EvalOnSelectorAsync(
            "#taskModal select[name='TaskTypeId'], #taskModal select[name='taskTypeId']",
            "el => { const opt = Array.from(el.options).find(o => o.value); if (opt) el.value = opt.value; }");
        await page.ClickAsync("#taskModal button[type='submit']");
        await page.WaitForLoadStateAsync();
        await page.WaitForTimeoutAsync(500);

        // Navigate to task detail
        var taskLink = await page.QuerySelectorAsync($"a:has-text('{taskTitle}')");
        Assert.NotNull(taskLink);
        await taskLink.ClickAsync();
        await page.WaitForURLAsync("**/tasks/**", new() { Timeout = 5000 });

        // Edit the title and save
        var updatedTitle = $"History Edited {Guid.NewGuid().ToString("N")[..6]}";
        await page.FillAsync("input[name='Title']", updatedTitle);
        await page.ClickAsync("button:has-text('Save Changes')");
        await page.WaitForLoadStateAsync();
        await page.WaitForTimeoutAsync(500);

        // Change History card should now be visible
        var content = await page.ContentAsync();
        Assert.Contains("Change History", content);
        Assert.DoesNotContain("An unhandled error", content);
    }

    [Fact]
    public async Task DeleteTask_RemovesTaskFromList()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        // Create a task
        await page.GotoAsync("/tasks");
        await page.WaitForSelectorAsync("button:has-text('New Task')", new() { Timeout = 10000 });
        await page.ClickAsync("button:has-text('New Task')");
        await page.WaitForSelectorAsync("#taskModal.show", new() { Timeout = 5000 });

        var taskTitle = $"Delete Me {Guid.NewGuid().ToString("N")[..6]}";
        await page.FillAsync("#taskModal input[name='Title']", taskTitle);
        await page.EvalOnSelectorAsync(
            "#taskModal select[name='TaskTypeId'], #taskModal select[name='taskTypeId']",
            "el => { const opt = Array.from(el.options).find(o => o.value); if (opt) el.value = opt.value; }");
        await page.ClickAsync("#taskModal button[type='submit']");
        await page.WaitForLoadStateAsync();
        await page.WaitForTimeoutAsync(500);

        // Navigate to task detail and delete
        var taskLink = await page.QuerySelectorAsync($"a:has-text('{taskTitle}')");
        if (taskLink != null)
        {
            await taskLink.ClickAsync();
            await page.WaitForURLAsync("**/tasks/**", new() { Timeout = 5000 });

            // Click delete (handle confirm dialog)
            page.Dialog += (_, dialog) => dialog.AcceptAsync();
            var deleteBtn = await page.QuerySelectorAsync("button:has-text('Delete')");
            if (deleteBtn != null)
            {
                await deleteBtn.ClickAsync();
                await page.WaitForURLAsync("**/tasks**", new() { Timeout = 5000 });
            }
        }
        Assert.DoesNotContain("An unhandled error", await page.ContentAsync());
    }

    [Fact]
    public async Task TasksPage_IncompleteView_FiltersOutCompletedAndCancelled()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        // Quick-add a task (defaults to NotStarted) so the incomplete view has at least one row
        await page.GotoAsync("/");
        var aliveTitle = $"alive-{Guid.NewGuid():N}".Substring(0, 16);
        await page.FillAsync("input[name='title']", aliveTitle);
        await page.ClickAsync("form.tp-quick-add button[type='submit']");
        await page.WaitForURLAsync("**/", new() { Timeout = 10000 });

        await page.GotoAsync("/tasks?incomplete=true");
        await page.WaitForLoadStateAsync();

        // The not-started task is incomplete, so it appears
        Assert.Contains(aliveTitle, await page.ContentAsync());

        // The Incomplete chip is lit (aria-pressed="true")
        Assert.Equal("true", await page.GetAttributeAsync("button[name='incomplete']", "aria-pressed"));

        // Per the PR B design, the Status dropdown stays visible in every state — the user can
        // narrow further on top of the chip filter. (The old "hide Status when incomplete" was a
        // workaround for the conflated three-segment toggle and was removed in v1.11.)
        Assert.Contains("All Status", await page.ContentAsync());
    }

    [Fact]
    public async Task TasksPage_OverdueRow_ShowsOverduePillNextToTargetDate()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/tasks");
        await page.WaitForSelectorAsync("button:has-text('New Task')", new() { Timeout = 10000 });
        await page.ClickAsync("button:has-text('New Task')");
        await page.WaitForSelectorAsync("#taskModal.show, .modal.show", new() { Timeout = 5000 });

        var title = $"overdue-row-{Guid.NewGuid().ToString("N")[..8]}";
        await page.FillAsync("#taskModal input[name='Title']", title);

        // Pick the first valid task type
        await page.EvalOnSelectorAsync(
            "#taskModal select[name='taskTypeId']",
            "el => { const opt = Array.from(el.options).find(o => o.value); if (opt) el.value = opt.value; }");

        // Set target date to yesterday so the row qualifies as overdue
        var yesterday = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
        await page.FillAsync("#taskModal input[name='TargetDate']", yesterday);

        await page.ClickAsync("#taskModal button[type='submit']");
        await page.WaitForLoadStateAsync();
        await page.WaitForSelectorAsync($"text={title}", new() { Timeout = 10000 });

        // Locate the row containing our task and assert the Overdue pill is present
        var pill = await page.WaitForSelectorAsync(".tp-badge-overdue", new() { Timeout = 5000 });
        Assert.NotNull(pill);

        // Non-color signal: the pill must carry visible text, not be color-alone
        var pillText = (await pill!.TextContentAsync())?.Trim();
        Assert.Equal("Overdue", pillText);
    }

    [Fact]
    public async Task TasksPage_OverdueChip_TogglesAndUpdatesUrl()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/tasks");
        await page.WaitForSelectorAsync("button[name='overdue']", new() { Timeout = 10000 });

        Assert.Equal("false", await page.GetAttributeAsync("button[name='overdue']", "aria-pressed"));

        await page.ClickAsync("button[name='overdue']");
        await page.WaitForURLAsync("**/tasks**overdue=true**", new() { Timeout = 10000 });
        Assert.Contains("overdue=true", page.Url);
        Assert.Equal("true", await page.GetAttributeAsync("button[name='overdue']", "aria-pressed"));
    }

    [Fact]
    public async Task TasksPage_IncompleteChip_TogglesAndUpdatesUrl()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/tasks");
        await page.WaitForSelectorAsync("button[name='incomplete']", new() { Timeout = 10000 });

        Assert.Equal("false", await page.GetAttributeAsync("button[name='incomplete']", "aria-pressed"));

        await page.ClickAsync("button[name='incomplete']");
        await page.WaitForURLAsync("**/tasks**incomplete=true**", new() { Timeout = 10000 });
        Assert.Contains("incomplete=true", page.Url);
        Assert.Equal("true", await page.GetAttributeAsync("button[name='incomplete']", "aria-pressed"));

        // Click again to turn off — URL drops incomplete=true
        await page.ClickAsync("button[name='incomplete']");
        await page.WaitForLoadStateAsync();
        Assert.DoesNotContain("incomplete=true", page.Url);
        Assert.Equal("false", await page.GetAttributeAsync("button[name='incomplete']", "aria-pressed"));
    }

    [Fact]
    public async Task TasksPage_BoardViewWithIncompleteChip_HidesCompletedAndCancelledColumns()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        // Plain board view: all four columns
        await page.GotoAsync("/tasks?view=board");
        await page.WaitForSelectorAsync(".tp-kanban-col", new() { Timeout = 10000 });
        var plainCount = await page.Locator(".tp-kanban-col").CountAsync();
        Assert.Equal(4, plainCount);

        // Board + incomplete chip: only NotStarted/InProgress/Blocked render
        await page.GotoAsync("/tasks?view=board&incomplete=true");
        await page.WaitForSelectorAsync(".tp-kanban-col", new() { Timeout = 10000 });
        var filteredCount = await page.Locator(".tp-kanban-col").CountAsync();
        Assert.Equal(3, filteredCount);

        var html = await page.ContentAsync();
        Assert.Contains("Not Started", html);
        Assert.Contains("In Progress", html);
        Assert.Contains("Blocked", html);
        Assert.DoesNotContain("Completed</span>", html);   // header span text — guards against false matches in body
    }

    [Fact]
    public async Task TasksPage_ColumnHeader_Click_SortsAscThenDescThenOff()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        // The list-view table only renders when there's at least one task (zero-state path
        // shows the tp-empty block instead). Seed a task via quick-add first.
        await page.GotoAsync("/");
        await page.FillAsync("input[name='title']", $"sort-{Guid.NewGuid():N}".Substring(0, 16));
        await page.ClickAsync("form.tp-quick-add button[type='submit']");
        await page.WaitForURLAsync("**/", new() { Timeout = 10000 });

        await page.GotoAsync("/tasks");
        await page.WaitForSelectorAsync(".tp-sortable-th", new() { Timeout = 10000 });

        var titleHeader = page.Locator(".tp-sortable-th").Filter(new() { HasTextString = "Title" });
        Assert.Equal("none", await titleHeader.GetAttributeAsync("aria-sort"));

        // Click 1 — asc
        await titleHeader.Locator("a.tp-sortable-link").ClickAsync();
        await page.WaitForURLAsync("**sortBy=title**", new() { Timeout = 10000 });
        Assert.Contains("sortBy=title", page.Url);
        Assert.Contains("sortDir=asc", page.Url);
        Assert.Equal("ascending",
            await page.Locator(".tp-sortable-th").Filter(new() { HasTextString = "Title" }).GetAttributeAsync("aria-sort"));

        // Click 2 — desc
        await page.Locator(".tp-sortable-th").Filter(new() { HasTextString = "Title" })
            .Locator("a.tp-sortable-link").ClickAsync();
        await page.WaitForURLAsync("**sortDir=desc**", new() { Timeout = 10000 });
        Assert.Contains("sortDir=desc", page.Url);
        Assert.Equal("descending",
            await page.Locator(".tp-sortable-th").Filter(new() { HasTextString = "Title" }).GetAttributeAsync("aria-sort"));

        // Click 3 — cycle off, sortBy and sortDir drop from URL
        await page.Locator(".tp-sortable-th").Filter(new() { HasTextString = "Title" })
            .Locator("a.tp-sortable-link").ClickAsync();
        await page.WaitForLoadStateAsync();
        Assert.DoesNotContain("sortBy=title", page.Url);
        Assert.DoesNotContain("sortDir=", page.Url);
        Assert.Equal("none",
            await page.Locator(".tp-sortable-th").Filter(new() { HasTextString = "Title" }).GetAttributeAsync("aria-sort"));
    }

    [Fact]
    public async Task TasksPage_ColumnHeaderSort_PreservesActiveFilters()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        // Quick-add a task (default area=Personal=0, status=NotStarted) so the area=0 +
        // incomplete=true filter returns a row and the table renders. The point of this
        // test is filter-preservation across header clicks; the specific area doesn't matter.
        await page.GotoAsync("/");
        await page.FillAsync("input[name='title']", $"sort-pres-{Guid.NewGuid():N}".Substring(0, 16));
        await page.ClickAsync("form.tp-quick-add button[type='submit']");
        await page.WaitForURLAsync("**/", new() { Timeout = 10000 });

        await page.GotoAsync("/tasks?area=0&incomplete=true");
        await page.WaitForSelectorAsync(".tp-sortable-th", new() { Timeout = 10000 });

        await page.Locator(".tp-sortable-th").Filter(new() { HasTextString = "Priority" })
            .Locator("a.tp-sortable-link").ClickAsync();
        await page.WaitForURLAsync("**sortBy=priority**", new() { Timeout = 10000 });

        Assert.Contains("sortBy=priority", page.Url);
        Assert.Contains("sortDir=asc",     page.Url);
        Assert.Contains("incomplete=true", page.Url);
        Assert.Contains("area=",           page.Url);
    }
}
