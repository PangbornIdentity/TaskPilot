namespace TaskPilot.Tests.E2E.Auth;

[Collection("Playwright")]
public class AuthTests(PlaywrightFixture fixture)
{
    [Fact]
    public async Task Register_NewUser_RedirectsToDashboard()
    {
        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync("/auth/register");
        await page.WaitForSelectorAsync("input[type='email']", new() { Timeout = 15000 });

        var email = $"reg_{Guid.NewGuid():N}@test.com";
        await page.FillAsync("input[type='email']", email);
        await page.FillAsync("input[type='password']", PlaywrightFixture.TestPassword);
        await page.ClickAsync("button[type='submit']");

        await page.WaitForURLAsync("**/", new() { Timeout = 15000 });
        Assert.Contains("/", page.Url);
    }

    [Fact]
    public async Task Login_ValidCredentials_RedirectsToDashboard()
    {
        // Register first
        await using var setupCtx = await fixture.NewContextAsync();
        var setupPage = await setupCtx.NewPageAsync();
        var email = $"login_{Guid.NewGuid():N}@test.com";
        await setupPage.GotoAsync("/auth/register");
        await setupPage.WaitForSelectorAsync("input[type='email']", new() { Timeout = 15000 });
        await setupPage.FillAsync("input[type='email']", email);
        await setupPage.FillAsync("input[type='password']", PlaywrightFixture.TestPassword);
        await setupPage.ClickAsync("button[type='submit']");
        await setupPage.WaitForURLAsync("**/", new() { Timeout = 15000 });

        // Login fresh
        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("/auth/login");
        await page.WaitForSelectorAsync("input[type='email']", new() { Timeout = 15000 });
        await page.FillAsync("input[type='email']", email);
        await page.FillAsync("input[type='password']", PlaywrightFixture.TestPassword);
        await page.ClickAsync("button[type='submit']");

        await page.WaitForURLAsync("**/", new() { Timeout = 15000 });
        Assert.DoesNotContain("/auth/login", page.Url);
    }

    [Fact]
    public async Task Login_WrongPassword_ShowsError()
    {
        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync("/auth/login");
        await page.WaitForSelectorAsync("input[type='email']", new() { Timeout = 15000 });
        await page.FillAsync("input[type='email']", "nobody@test.com");
        await page.FillAsync("input[type='password']", "wrongpassword");
        await page.ClickAsync("button[type='submit']");

        await page.WaitForTimeoutAsync(1000);
        Assert.Contains("/auth/login", page.Url);
        var content = await page.ContentAsync();
        Assert.True(content.Contains("Invalid") || content.Contains("invalid") || content.Contains("error"),
            "Expected error message on bad login");
    }

    [Fact]
    public async Task UnauthenticatedUser_RedirectedToLogin()
    {
        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync("/");
        await page.WaitForURLAsync("**/auth/login**", new() { Timeout = 15000 });
        Assert.Contains("auth/login", page.Url);
    }

    [Fact]
    public async Task Logout_ClearsSession_RedirectsToLogin()
    {
        var (context, page, _) = await fixture.NewAuthenticatedPageAsync();
        await using var _ = context;

        var logoutBtn = await page.QuerySelectorAsync("button:has-text('Logout'), form[action='/auth/logout'] button");
        Assert.NotNull(logoutBtn);
        await logoutBtn.ClickAsync();

        await page.WaitForURLAsync("**/auth/login**", new() { Timeout = 10000 });
        Assert.Contains("auth/login", page.Url);
    }
}
