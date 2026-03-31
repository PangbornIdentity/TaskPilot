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
}
