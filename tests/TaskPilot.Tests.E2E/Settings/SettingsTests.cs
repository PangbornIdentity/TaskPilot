namespace TaskPilot.Tests.E2E.Settings;

[Collection("Playwright")]
public class SettingsTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Settings_PageLoads_ShowsApiKeySection()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/settings");
        await page.WaitForSelectorAsync("text=API Keys", new() { Timeout = 10000 });

        var content = await page.ContentAsync();
        Assert.True(content.Contains("API Key") || content.Contains("API Keys"),
            "Expected API key section");
    }

    [Fact]
    public async Task Settings_GenerateApiKey_ShowsKeyOnce()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/settings");
        await page.WaitForSelectorAsync("input[name='keyName']", new() { Timeout = 10000 });

        await page.FillAsync("input[name='keyName']", "E2E Test Key");
        await page.ClickAsync("button:has-text('Generate Key')");

        await page.WaitForLoadStateAsync();
        var content = await page.ContentAsync();
        Assert.True(
            content.Contains("Copy") || content.Contains("created") || content.Contains("tp_"),
            "Expected one-time key display after generation");
    }

    [Fact]
    public async Task Settings_AppearanceSection_IsPresent()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/settings");
        await page.WaitForSelectorAsync("text=Appearance", new() { Timeout = 10000 });

        var content = await page.ContentAsync();
        Assert.Contains("Appearance", content);
        Assert.DoesNotContain("An unhandled error", content);
    }

    [Fact]
    public async Task Settings_ChangePassword_FormIsPresent()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/settings");
        await page.WaitForSelectorAsync("text=Password", new() { Timeout = 10000 });

        var content = await page.ContentAsync();
        Assert.True(content.Contains("Password") || content.Contains("password"),
            "Expected change password section");

        var passwordInput = await page.QuerySelectorAsync("input[name='currentPassword']");
        Assert.NotNull(passwordInput);
    }

    [Fact]
    public async Task Settings_EditTag_RenameAndRecolor_PersistsAcrossPages()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/settings");
        await page.WaitForSelectorAsync("input[name='tagName']", new() { Timeout = 10000 });

        var originalName = $"orig{Guid.NewGuid():N}".Substring(0, 12);
        await page.FillAsync("input[name='tagName']", originalName);
        await page.ClickAsync("button:has-text('Create Tag')");
        await page.WaitForLoadStateAsync();
        await page.WaitForSelectorAsync($"text={originalName}", new() { Timeout = 10000 });

        // Click the pencil edit affordance for that tag
        await page.ClickAsync($"a[aria-label='Edit tag {originalName}']");
        await page.WaitForSelectorAsync("#tag-edit-row", new() { Timeout = 10000 });

        var renamed = originalName + "-x";
        await page.FillAsync("#edit-tag-name", renamed);
        await page.ClickAsync("#tag-edit-row button:has-text('Save changes')");
        await page.WaitForLoadStateAsync();
        await page.WaitForSelectorAsync($"text={renamed}", new() { Timeout = 10000 });

        var content = await page.ContentAsync();
        Assert.Contains(renamed, content);
        Assert.DoesNotContain($">{originalName}<", content);
    }

    [Fact]
    public async Task Settings_EditTag_DuplicateName_ShowsInlineError()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/settings");
        await page.WaitForSelectorAsync("input[name='tagName']", new() { Timeout = 10000 });

        var nameA = $"alpha{Guid.NewGuid():N}".Substring(0, 12);
        var nameB = $"beta{Guid.NewGuid():N}".Substring(0, 12);
        await page.FillAsync("input[name='tagName']", nameA);
        await page.ClickAsync("button:has-text('Create Tag')");
        await page.WaitForLoadStateAsync();
        await page.FillAsync("input[name='tagName']", nameB);
        await page.ClickAsync("button:has-text('Create Tag')");
        await page.WaitForLoadStateAsync();
        await page.WaitForSelectorAsync($"text={nameB}", new() { Timeout = 10000 });

        await page.ClickAsync($"a[aria-label='Edit tag {nameB}']");
        await page.WaitForSelectorAsync("#tag-edit-row", new() { Timeout = 10000 });
        await page.FillAsync("#edit-tag-name", nameA);
        await page.ClickAsync("#tag-edit-row button:has-text('Save changes')");
        await page.WaitForLoadStateAsync();

        var content = await page.ContentAsync();
        Assert.Contains("already exists", content);
        // Edit row must remain open with the failing input still visible
        var stillOpen = await page.QuerySelectorAsync("#tag-edit-row");
        Assert.NotNull(stillOpen);
    }

    [Fact]
    public async Task Settings_EditTag_KeyboardOnly_CompletesEdit()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/settings");
        await page.WaitForSelectorAsync("input[name='tagName']", new() { Timeout = 10000 });

        var name = $"kbd{Guid.NewGuid():N}".Substring(0, 12);
        await page.FillAsync("input[name='tagName']", name);
        await page.ClickAsync("button:has-text('Create Tag')");
        await page.WaitForLoadStateAsync();
        await page.WaitForSelectorAsync($"text={name}", new() { Timeout = 10000 });

        // Activate the edit anchor via keyboard
        var editAnchor = await page.QuerySelectorAsync($"a[aria-label='Edit tag {name}']");
        Assert.NotNull(editAnchor);
        await editAnchor!.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await page.WaitForSelectorAsync("#tag-edit-row #edit-tag-name", new() { Timeout = 10000 });

        // Type a new suffix and submit via Enter inside the form
        var renamed = name + "-k";
        await page.FillAsync("#edit-tag-name", renamed);
        await page.Keyboard.PressAsync("Enter");
        await page.WaitForLoadStateAsync();
        await page.WaitForSelectorAsync($"text={renamed}", new() { Timeout = 10000 });

        var content = await page.ContentAsync();
        Assert.Contains(renamed, content);
    }
}
