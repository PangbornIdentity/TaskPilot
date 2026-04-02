namespace TaskPilot.Tests.E2E.Integrations;

[Collection("Playwright")]
public class IntegrationsPageTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task IntegrationsPage_AuthenticatedUser_LoadsWithoutError()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/integrations");
        await page.WaitForSelectorAsync(".tp-page-title", new() { Timeout = 10000 });

        var content = await page.ContentAsync();
        Assert.DoesNotContain("An unhandled error", content);
        Assert.DoesNotContain("404", content);
        Assert.Contains("Integrations", content);
    }

    [Fact]
    public async Task IntegrationsPage_UnauthenticatedUser_RedirectsToLogin()
    {
        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync("/integrations");
        await page.WaitForURLAsync("**/auth/login**", new() { Timeout = 10000 });

        Assert.Contains("auth/login", page.Url);
    }

    [Fact]
    public async Task IntegrationsPage_ContainsApiKeySection()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/integrations");
        await page.WaitForSelectorAsync(".tp-page-title", new() { Timeout = 10000 });

        var content = await page.ContentAsync();
        Assert.True(
            content.Contains("API key") || content.Contains("X-Api-Key") || content.Contains("Quick Start"),
            "Expected API key instructions section on Integrations page");
    }

    [Fact]
    public async Task IntegrationsPage_ContainsCurlExample()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/integrations");
        await page.WaitForSelectorAsync(".tp-page-title", new() { Timeout = 10000 });

        var content = await page.ContentAsync();
        Assert.Contains("curl", content);
    }

    [Fact]
    public async Task IntegrationsPage_ContainsClaudeToolDefinition()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/integrations");
        await page.WaitForSelectorAsync(".tp-page-title", new() { Timeout = 10000 });

        var content = await page.ContentAsync();
        Assert.True(
            content.Contains("input_schema") || content.Contains("Claude"),
            "Expected Claude tool definition section on Integrations page");
    }

    [Fact]
    public async Task IntegrationsPage_ContainsOpenAiToolDefinition()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/integrations");
        await page.WaitForSelectorAsync(".tp-page-title", new() { Timeout = 10000 });

        var content = await page.ContentAsync();
        Assert.True(
            content.Contains("OpenAI") || content.Contains("GPT") || content.Contains("\"type\": \"function\""),
            "Expected OpenAI function definition section on Integrations page");
    }

    [Fact]
    public async Task IntegrationsPage_ContainsMcpComingSoon()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/integrations");
        await page.WaitForSelectorAsync(".tp-page-title", new() { Timeout = 10000 });

        var content = await page.ContentAsync();
        Assert.True(
            content.Contains("Coming Soon") || content.Contains("coming soon") || content.Contains("MCP"),
            "Expected MCP coming soon section on Integrations page");
    }

    [Fact]
    public async Task IntegrationsPage_SwaggerLink_IsPresent()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/integrations");
        await page.WaitForSelectorAsync(".tp-page-title", new() { Timeout = 10000 });

        // In dev mode a Swagger link should appear; in non-dev a text reference to /swagger
        var content = await page.ContentAsync();
        Assert.True(
            content.Contains("/swagger") || content.Contains("swagger"),
            "Expected a reference to Swagger/API docs on Integrations page");
    }

    [Fact]
    public async Task IntegrationsPage_CopyButton_IsClickableWithoutError()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/integrations");
        await page.WaitForSelectorAsync(".tp-copy-btn", new() { Timeout = 10000 });

        // Grant clipboard permissions
        await context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);

        var copyBtn = await page.QuerySelectorAsync(".tp-copy-btn");
        Assert.NotNull(copyBtn);
        await copyBtn.ClickAsync();

        // Button should change to "Copied!" feedback
        await page.WaitForTimeoutAsync(300);
        var content = await page.ContentAsync();
        Assert.DoesNotContain("An unhandled error", content);
    }

    [Fact]
    public async Task NavSidebar_ContainsIntegrationsLink()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/");
        await page.WaitForSelectorAsync(".tp-sidebar", new() { Timeout = 10000 });

        var link = await page.QuerySelectorAsync("a[href='/integrations']");
        Assert.NotNull(link);
    }

    [Fact]
    public async Task SettingsPage_ContainsApiReferenceSection()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        await page.GotoAsync("/settings");
        await page.WaitForSelectorAsync(".tp-page-title", new() { Timeout = 10000 });

        var content = await page.ContentAsync();
        Assert.True(
            content.Contains("API Reference") || content.Contains("Integrations"),
            "Expected API Reference section on Settings page");
    }
}
