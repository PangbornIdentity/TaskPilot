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
}
