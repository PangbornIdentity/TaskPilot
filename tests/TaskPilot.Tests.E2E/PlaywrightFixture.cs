using Microsoft.Playwright;

namespace TaskPilot.Tests.E2E;

/// <summary>
/// Shared fixture: one browser instance per test collection.
/// Tests get a fresh BrowserContext (isolated cookies/storage) per test.
/// App must be running at BaseUrl before tests execute.
/// </summary>
public class PlaywrightFixture : IAsyncLifetime
{
    public const string BaseUrl = "http://localhost:5125";
    public const string TestPassword = "E2eTest123!";

    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            SlowMo = 0
        });
    }

    public async Task DisposeAsync()
    {
        await Browser.DisposeAsync();
        Playwright.Dispose();
    }

    public Task<IBrowserContext> NewContextAsync() =>
        Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseUrl,
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 }
        });

    /// <summary>
    /// Creates a context + page already logged in with a unique test user.
    /// Returns (context, page, email).
    /// </summary>
    public async Task<(IBrowserContext Context, IPage Page, string Email)> NewAuthenticatedPageAsync()
    {
        var context = await NewContextAsync();
        var page = await context.NewPageAsync();

        var email = $"e2e_{Guid.NewGuid():N}@taskpilot.test";

        await page.GotoAsync("/auth/register");
        await page.WaitForSelectorAsync("input[type='email']", new() { Timeout = 15000 });

        await page.FillAsync("input[type='email']", email);
        await page.FillAsync("input[type='password']", TestPassword);
        await page.ClickAsync("button[type='submit']");

        // After registration, server redirects to /
        await page.WaitForURLAsync("**/", new() { Timeout = 15000 });

        return (context, page, email);
    }
}

[CollectionDefinition("Playwright")]
public class PlaywrightCollection : ICollectionFixture<PlaywrightFixture> { }
