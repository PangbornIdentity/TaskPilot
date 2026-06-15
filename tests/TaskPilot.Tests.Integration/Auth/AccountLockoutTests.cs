using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TaskPilot.Tests.Integration.Auth;

/// <summary>
/// LDG-003 — Account lockout after 5 consecutive failed login attempts.
/// Identity is configured with:
///   MaxFailedAccessAttempts = 5
///   DefaultLockoutTimeSpan   = 15 minutes
///   AllowedForNewUsers       = true
///
/// After 5 bad-password attempts for a valid account the 6th attempt MUST fail
/// even when the correct password is supplied (Identity returns IsLockedOut).
///
/// AccountController.Login uses:
///   signInManager.PasswordSignInAsync(..., lockoutOnFailure: true)
/// which enables this flow.
///
/// These tests FAIL if lockoutOnFailure is set to false, MaxFailedAccessAttempts
/// is raised above 5, or AllowedForNewUsers is false.
/// </summary>
[Collection("Integration")]
public class AccountLockout_LDG_003_Tests : IClassFixture<TaskPilotWebAppFactory>
{
    private readonly TaskPilotWebAppFactory _factory;

    public AccountLockout_LDG_003_Tests(TaskPilotWebAppFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true
    });

    /// <summary>
    /// Core lockout test: 5 wrong passwords → 6th attempt with correct password → 401.
    /// </summary>
    // NFR-SEC-012
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Login_After5FailedAttempts_CorrectPasswordStillFails_LDG_003()
    {
        // Arrange — register a fresh user
        var email = $"lockout_{Guid.NewGuid():N}@test.com";
        const string correctPassword = "Correct!Pass1";

        var setupClient = CreateClient();
        var regResp = await setupClient.PostAsJsonAsync("/api/v1/account/register", new
        {
            Email = email,
            Password = correctPassword
        });
        regResp.EnsureSuccessStatusCode();

        // Act — fire 5 bad-password attempts
        // Each call must use a fresh client so we don't carry cookie state from a prior success.
        for (var i = 0; i < 5; i++)
        {
            var c = CreateClient();
            var r = await c.PostAsJsonAsync("/api/v1/account/login", new
            {
                Email = email,
                Password = "Wr0ng!Passw0rd"
            });
            // Each wrong-password attempt should be 401
            Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        }

        // Assert — 6th attempt with CORRECT password must be rejected (account locked)
        var lockClient = CreateClient();
        var lockResp = await lockClient.PostAsJsonAsync("/api/v1/account/login", new
        {
            Email = email,
            Password = correctPassword
        });

        Assert.Equal(HttpStatusCode.Unauthorized, lockResp.StatusCode);
    }

    /// <summary>
    /// Confirms that fewer than 5 failures do not lock the account.
    /// The 5th attempt with the correct password (after 4 bad ones) should succeed.
    /// </summary>
    // NFR-SEC-012
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Login_FourFailedAttempts_CorrectPasswordSucceeds_LDG_003()
    {
        var email = $"notlocked_{Guid.NewGuid():N}@test.com";
        const string correctPassword = "Correct!Pass1";

        var setupClient = CreateClient();
        var regResp = await setupClient.PostAsJsonAsync("/api/v1/account/register", new
        {
            Email = email,
            Password = correctPassword
        });
        regResp.EnsureSuccessStatusCode();

        // 4 bad-password attempts — one less than the threshold
        for (var i = 0; i < 4; i++)
        {
            var c = CreateClient();
            await c.PostAsJsonAsync("/api/v1/account/login", new
            {
                Email = email,
                Password = "Wr0ng!Passw0rd"
            });
        }

        // 5th attempt with correct password — must succeed (not yet locked)
        var goodClient = CreateClient();
        var goodResp = await goodClient.PostAsJsonAsync("/api/v1/account/login", new
        {
            Email = email,
            Password = correctPassword
        });

        Assert.Equal(HttpStatusCode.OK, goodResp.StatusCode);
    }
}
