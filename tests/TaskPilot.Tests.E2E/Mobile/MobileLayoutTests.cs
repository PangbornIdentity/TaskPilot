using Microsoft.Playwright;

namespace TaskPilot.Tests.E2E.Mobile;

[Collection("Playwright")]
public class MobileLayoutTests(PlaywrightFixture fixture)
{
    // iPhone 12 Pro viewport
    private static readonly ViewportSize MobileViewport = new() { Width = 390, Height = 844 };
    // iPad viewport (tablet rail range)
    private static readonly ViewportSize TabletViewport = new() { Width = 768, Height = 1024 };

    private async Task<(IBrowserContext Context, IPage Page)> NewMobilePageAsync()
    {
        var ctx = await fixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = PlaywrightFixture.BaseUrl,
            ViewportSize = MobileViewport,
            IgnoreHTTPSErrors = true
        });
        var page = await ctx.NewPageAsync();
        return (ctx, page);
    }

    private async Task<(IBrowserContext Context, IPage Page)> NewTabletPageAsync()
    {
        var ctx = await fixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = PlaywrightFixture.BaseUrl,
            ViewportSize = TabletViewport,
            IgnoreHTTPSErrors = true
        });
        var page = await ctx.NewPageAsync();
        return (ctx, page);
    }

    // MOB-001
    [Fact]
    public async Task MobileViewport_ShowsMobileHeader()
    {
        var (ctx, page) = await NewMobilePageAsync();
        try
        {
            var email = $"e2e_{Guid.NewGuid():N}@taskpilot.test";
            await page.GotoAsync("/auth/register");
            await page.FillAsync("input[type='email']", email);
            await page.FillAsync("input[type='password']", PlaywrightFixture.TestPassword);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForURLAsync("**/");

            var header = page.Locator(".tp-mobile-header");
            await Assertions.Expect(header).ToBeVisibleAsync();
        }
        finally { await ctx.DisposeAsync(); }
    }

    // MOB-002
    [Fact]
    public async Task MobileViewport_HamburgerButtonVisible()
    {
        var (ctx, page) = await NewMobilePageAsync();
        try
        {
            var email = $"e2e_{Guid.NewGuid():N}@taskpilot.test";
            await page.GotoAsync("/auth/register");
            await page.FillAsync("input[type='email']", email);
            await page.FillAsync("input[type='password']", PlaywrightFixture.TestPassword);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForURLAsync("**/");

            var hamburger = page.Locator(".tp-hamburger");
            await Assertions.Expect(hamburger).ToBeVisibleAsync();
        }
        finally { await ctx.DisposeAsync(); }
    }

    // MOB-003
    [Fact]
    public async Task MobileViewport_SidebarHiddenByDefault()
    {
        var (ctx, page) = await NewMobilePageAsync();
        try
        {
            var email = $"e2e_{Guid.NewGuid():N}@taskpilot.test";
            await page.GotoAsync("/auth/register");
            await page.FillAsync("input[type='email']", email);
            await page.FillAsync("input[type='password']", PlaywrightFixture.TestPassword);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForURLAsync("**/");

            // Sidebar should be off-screen (transform: translateX(-100%)) — not visible
            var sidebar = page.Locator(".tp-sidebar");
            var box = await sidebar.BoundingBoxAsync();
            // Either no bounding box or right edge <= 0 (off-screen to the left)
            Assert.True(box is null || box.X + box.Width <= 0,
                $"Sidebar should be off-screen on mobile. BoundingBox: x={box?.X}, w={box?.Width}");
        }
        finally { await ctx.DisposeAsync(); }
    }

    // MOB-004
    [Fact]
    public async Task MobileViewport_HamburgerOpensSidebar()
    {
        var (ctx, page) = await NewMobilePageAsync();
        try
        {
            var email = $"e2e_{Guid.NewGuid():N}@taskpilot.test";
            await page.GotoAsync("/auth/register");
            await page.FillAsync("input[type='email']", email);
            await page.FillAsync("input[type='password']", PlaywrightFixture.TestPassword);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForURLAsync("**/");

            await page.ClickAsync(".tp-hamburger");
            await page.WaitForTimeoutAsync(400); // transition duration

            var sidebar = page.Locator(".tp-sidebar");
            Assert.True(await sidebar.EvaluateAsync<bool>("el => el.classList.contains('open')"),
                "Sidebar should have 'open' class after hamburger click");
        }
        finally { await ctx.DisposeAsync(); }
    }

    // MOB-005
    [Fact]
    public async Task MobileViewport_BackdropVisibleWhenSidebarOpen()
    {
        var (ctx, page) = await NewMobilePageAsync();
        try
        {
            var email = $"e2e_{Guid.NewGuid():N}@taskpilot.test";
            await page.GotoAsync("/auth/register");
            await page.FillAsync("input[type='email']", email);
            await page.FillAsync("input[type='password']", PlaywrightFixture.TestPassword);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForURLAsync("**/");

            await page.ClickAsync(".tp-hamburger");
            await page.WaitForTimeoutAsync(400);

            var backdrop = page.Locator(".tp-sidebar-backdrop");
            Assert.True(await backdrop.EvaluateAsync<bool>("el => el.classList.contains('active')"),
                "Backdrop should be active when sidebar is open");
        }
        finally { await ctx.DisposeAsync(); }
    }

    // MOB-006
    [Fact]
    public async Task MobileViewport_BackdropClickClosesSidebar()
    {
        var (ctx, page) = await NewMobilePageAsync();
        try
        {
            var email = $"e2e_{Guid.NewGuid():N}@taskpilot.test";
            await page.GotoAsync("/auth/register");
            await page.FillAsync("input[type='email']", email);
            await page.FillAsync("input[type='password']", PlaywrightFixture.TestPassword);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForURLAsync("**/");

            await page.ClickAsync(".tp-hamburger");
            await page.WaitForTimeoutAsync(400);

            // Click the backdrop area to the right of the open sidebar (sidebar is 220px wide)
            await page.Mouse.ClickAsync(310, 400);
            await page.WaitForTimeoutAsync(400);

            var sidebar = page.Locator(".tp-sidebar");
            Assert.False(await sidebar.EvaluateAsync<bool>("el => el.classList.contains('open')"),
                "Sidebar should not have 'open' class after backdrop click");
        }
        finally { await ctx.DisposeAsync(); }
    }

    // MOB-007
    [Fact]
    public async Task MobileViewport_CanNavigateViaSidebar()
    {
        var (ctx, page) = await NewMobilePageAsync();
        try
        {
            var email = $"e2e_{Guid.NewGuid():N}@taskpilot.test";
            await page.GotoAsync("/auth/register");
            await page.FillAsync("input[type='email']", email);
            await page.FillAsync("input[type='password']", PlaywrightFixture.TestPassword);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForURLAsync("**/");

            // Open sidebar, click Tasks
            await page.ClickAsync(".tp-hamburger");
            await page.WaitForTimeoutAsync(400);
            await page.ClickAsync(".tp-nav-link[href='/tasks']");
            await page.WaitForURLAsync("**/tasks**");

            Assert.Contains("/tasks", page.Url);
        }
        finally { await ctx.DisposeAsync(); }
    }

    // MOB-008
    [Fact]
    public async Task TabletViewport_SidebarIconRailVisible()
    {
        var (ctx, page) = await NewTabletPageAsync();
        try
        {
            var email = $"e2e_{Guid.NewGuid():N}@taskpilot.test";
            await page.GotoAsync("/auth/register");
            await page.FillAsync("input[type='email']", email);
            await page.FillAsync("input[type='password']", PlaywrightFixture.TestPassword);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForURLAsync("**/");

            // Sidebar should be visible (in flow, not fixed)
            var sidebar = page.Locator(".tp-sidebar");
            await Assertions.Expect(sidebar).ToBeVisibleAsync();

            // Brand text should be hidden (icon-only rail)
            var brandText = page.Locator(".tp-brand-text");
            await Assertions.Expect(brandText).ToBeHiddenAsync();
        }
        finally { await ctx.DisposeAsync(); }
    }

    // MOB-009
    [Fact]
    public async Task TabletViewport_NoHamburgerButton()
    {
        var (ctx, page) = await NewTabletPageAsync();
        try
        {
            var email = $"e2e_{Guid.NewGuid():N}@taskpilot.test";
            await page.GotoAsync("/auth/register");
            await page.FillAsync("input[type='email']", email);
            await page.FillAsync("input[type='password']", PlaywrightFixture.TestPassword);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForURLAsync("**/");

            // Mobile header should not be visible on tablet
            var mobileHeader = page.Locator(".tp-mobile-header");
            await Assertions.Expect(mobileHeader).ToBeHiddenAsync();
        }
        finally { await ctx.DisposeAsync(); }
    }

    // MOB-010
    [Fact]
    public async Task MobileViewport_ChangelogShowsV17()
    {
        var (ctx, page) = await NewMobilePageAsync();
        try
        {
            var email = $"e2e_{Guid.NewGuid():N}@taskpilot.test";
            await page.GotoAsync("/auth/register");
            await page.FillAsync("input[type='email']", email);
            await page.FillAsync("input[type='password']", PlaywrightFixture.TestPassword);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForURLAsync("**/");

            // Open sidebar and navigate to changelog
            await page.ClickAsync(".tp-hamburger");
            await page.WaitForTimeoutAsync(400);
            await page.ClickAsync(".tp-changelog-nav-link");
            await page.WaitForURLAsync("**/changelog**");

            var content = await page.ContentAsync();
            Assert.Contains("1.7", content);
        }
        finally { await ctx.DisposeAsync(); }
    }
}
