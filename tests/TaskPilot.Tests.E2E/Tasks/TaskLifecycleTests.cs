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

    // ───────── Regression guards (v1.11 hotfix) ─────────
    //
    // The sortable column headers introduced in v1.11 originally rendered an inactive
    // chevron (bi-chevron-expand) on every sortable th. With 6 sortable columns that
    // added ~90px of width to a table that already used the .tp-table-scroll wrapper
    // for narrow-viewport overflow. The result: a horizontal scrollbar appeared at
    // viewports that previously fit the table, including standard desktop widths.
    // The fix was to drop the inactive chevron entirely and rely on cursor-pointer +
    // hover color change for the "click me" affordance.
    //
    // These two tests guard against the regression coming back:

    [Fact]
    public async Task TasksPage_ListView_DoesNotOverflowHorizontallyAtDesktopWidth()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        // PlaywrightFixture default viewport is 1280×800 (a comfortable desktop width).
        // The Tasks list now uses a single <table class="tp-table"> that reflows responsively
        // (CSS Grid below 640px, Bootstrap d-{bp}-table-cell utilities for column hiding).
        // At 1280px the full table MUST fit the viewport — assert against document scrollWidth
        // so this catches any future change that pushes the page horizontally.
        await page.GotoAsync("/");
        await page.FillAsync("input[name='title']",
            $"overflow-{Guid.NewGuid().ToString("N")[..8]}");
        await page.ClickAsync("form.tp-quick-add button[type='submit']");
        await page.WaitForURLAsync("**/", new() { Timeout = 10000 });

        await page.GotoAsync("/tasks");
        await page.WaitForSelectorAsync(".tp-table", new() { Timeout = 10000 });

        var dimensions = await page.EvaluateAsync<long[]>(@"() => {
            return [document.documentElement.scrollWidth, window.innerWidth];
        }");
        var docScrollWidth = dimensions[0];
        var viewport = dimensions[1];

        // +1 for sub-pixel rounding tolerance.
        Assert.True(docScrollWidth <= viewport + 1,
            $"Tasks list page overflowed horizontally at {viewport}px viewport: " +
            $"document.scrollWidth={docScrollWidth}px. Likely cause: a column-header / cell change " +
            $"widened the table past the viewport. Check the SortableHeader markup, .tp-sortable-link " +
            $"padding, any new th content (icons, chips), or accidental removal of d-none/d-{{bp}}-table-cell utilities.");
    }

    // Parameterized responsiveness audit. Each row asserts no horizontal overflow on the
    // tasks list at a specific viewport width. Today (pre-responsive-overhaul) this fails
    // at narrow widths — the test exists as a SPEC the implementation must reach, not a
    // green "all is well" check. WCAG 1.4.10 (Reflow) requires no horizontal scroll on
    // vertical-scrolling content at 320px width; we go further and require it at every
    // common breakpoint we care about. The proper fix is a card-stack mobile layout +
    // responsive column hiding on tablet — see PR comments for the design discussion.
    [Theory]
    [InlineData(320,  "mobile  XS  (iPhone SE 1st gen)")]
    [InlineData(375,  "mobile  S   (iPhone SE / 8)")]
    [InlineData(414,  "mobile  M   (iPhone Pro Max)")]
    [InlineData(540,  "mobile  L")]
    [InlineData(640,  "mobile/tablet boundary")]
    [InlineData(768,  "tablet  S   (iPad portrait)")]
    [InlineData(900,  "tablet  M")]
    [InlineData(1024, "tablet  L / desktop entry (iPad landscape)")]
    [InlineData(1280, "desktop M  (Playwright fixture default)")]
    [InlineData(1440, "desktop L")]
    public async Task TasksPage_TableDoesNotOverflow_AtViewport(int width, string label)
    {
        var context = await fixture.Browser.NewContextAsync(new()
        {
            BaseURL = PlaywrightFixture.BaseUrl,
            IgnoreHTTPSErrors = true,
            ViewportSize = new() { Width = width, Height = 800 }
        });
        await using var _ = context;
        var page = await context.NewPageAsync();

        // Register + log in for this viewport-specific context
        var email = $"viewport_{width}_{Guid.NewGuid().ToString("N")[..8]}@taskpilot.test";
        await page.GotoAsync("/auth/register");
        await page.WaitForSelectorAsync("input[type='email']", new() { Timeout = 15000 });
        await page.FillAsync("input[type='email']", email);
        await page.FillAsync("input[type='password']", PlaywrightFixture.TestPassword);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForURLAsync("**/", new() { Timeout = 15000 });

        // Seed 10 realistic-length rows so the table has actual content, not a single short row
        for (var i = 1; i <= 10; i++)
        {
            await page.FillAsync("input[name='title']",
                $"row {i} with a realistically long task title for width testing");
            await page.ClickAsync("form.tp-quick-add button[type='submit']");
            await page.WaitForURLAsync("**/", new() { Timeout = 10000 });
        }

        await page.GotoAsync("/tasks");
        await page.WaitForSelectorAsync(".tp-table", new() { Timeout = 10000 });

        // The honest WCAG 1.4.10 check is whether the *document* horizontally overflows the viewport.
        // The Tasks page renders a single <table class="tp-table"> at every breakpoint and reflows
        // to a CSS-Grid card-like stack below 640px (no wrapper, no horizontal scroll).
        var dimensions = await page.EvaluateAsync<long[]>(@"() => {
            const tbl = document.querySelector('.tp-table');
            return [tbl.scrollWidth, tbl.clientWidth, document.documentElement.scrollWidth, window.innerWidth];
        }");
        var tableScrollWidth = dimensions[0];
        var tableClientWidth = dimensions[1];
        var docScrollWidth   = dimensions[2];
        var viewport         = dimensions[3];

        var docOverflow = docScrollWidth - viewport;

        // +1 tolerance for sub-pixel rounding. Document-level overflow is what users feel as
        // a horizontal scrollbar — this is the WCAG 1.4.10 (Reflow) signal we care about.
        Assert.True(docOverflow <= 1,
            $"[{label}] @ {width}px viewport: page overflowed horizontally by {docOverflow}px " +
            $"(document.scrollWidth={docScrollWidth}, viewport={viewport}, " +
            $"table.scrollWidth={tableScrollWidth}, table.clientWidth={tableClientWidth}). " +
            $"Horizontal scroll is a WCAG 1.4.10 violation at <=320px and a usability problem above. " +
            $"Expected: single tp-table reflows via CSS Grid below 640px and via Bootstrap d-none/d-{{bp}}-table-cell utilities at tablet/desktop.");
    }

    [Fact]
    public async Task TasksPage_InactiveSortableHeader_HasNoChevronIcon()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        // Seed a task so the table renders (zero-state path renders tp-empty, no thead).
        await page.GotoAsync("/");
        await page.FillAsync("input[name='title']",
            $"chev-{Guid.NewGuid().ToString("N")[..6]}");
        await page.ClickAsync("form.tp-quick-add button[type='submit']");
        await page.WaitForURLAsync("**/", new() { Timeout = 10000 });

        // No sortBy in URL → no header is active → ALL six sortable headers are inactive.
        // None of them should render a chevron. The active state still shows
        // bi-chevron-up / bi-chevron-down — see the second half of this test.
        await page.GotoAsync("/tasks");
        await page.WaitForSelectorAsync(".tp-sortable-th", new() { Timeout = 10000 });

        var totalSortable = await page.Locator(".tp-sortable-th").CountAsync();
        var anyChevron = await page.Locator(".tp-sortable-th i.bi[class*='chevron']").CountAsync();
        Assert.True(totalSortable >= 6, $"Expected ≥6 sortable headers, found {totalSortable}");
        Assert.Equal(0, anyChevron);

        // Now activate the Priority sort and verify the chevron returns on JUST that header
        // (no other inactive headers grow chevrons as a side-effect).
        await page.GotoAsync("/tasks?sortBy=priority&sortDir=asc");
        await page.WaitForSelectorAsync(".tp-sortable-th.tp-sortable-active", new() { Timeout = 10000 });

        var activeChevrons = await page.Locator(".tp-sortable-th.tp-sortable-active i.bi-chevron-up").CountAsync();
        var inactiveChevrons = await page.Locator(".tp-sortable-th:not(.tp-sortable-active) i.bi[class*='chevron']").CountAsync();
        Assert.Equal(1, activeChevrons);
        Assert.Equal(0, inactiveChevrons);
    }
}
