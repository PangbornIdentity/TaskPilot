using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TaskPilot.Tests.Integration.Auth;

/// <summary>
/// LDG-002 — Password policy (RequiredLength=10, RequireUppercase=true,
/// RequireNonAlphanumeric=true, RequireDigit=true, RequireLowercase=true)
/// These tests FAIL if the Identity password options are relaxed (e.g. length
/// back to 6 or uppercase/non-alphanumeric requirements dropped).
/// </summary>
[Collection("Integration")]
public class PasswordPolicy_LDG_002_Tests : IClassFixture<TaskPilotWebAppFactory>
{
    private readonly TaskPilotWebAppFactory _factory;

    public PasswordPolicy_LDG_002_Tests(TaskPilotWebAppFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true
    });

    // ── Weak password rejections ──────────────────────────────────────────────

    // NFR-SEC-011
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Register_NineCharPassword_Returns400_LDG_002()
    {
        // Length = 9 < 10 required — even if all other rules met
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/account/register", new
        {
            Email = $"ldg002a_{Guid.NewGuid():N}@test.com",
            Password = "Pass1!abc"  // 9 chars, has upper, digit, special, lower
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // NFR-SEC-011
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Register_NoUppercasePassword_Returns400_LDG_002()
    {
        // 10+ chars, digit, special, but NO uppercase
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/account/register", new
        {
            Email = $"ldg002b_{Guid.NewGuid():N}@test.com",
            Password = "password1!"  // 10 chars, all lowercase
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // NFR-SEC-011
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Register_NoNonAlphanumericPassword_Returns400_LDG_002()
    {
        // 10+ chars, uppercase, digit, lowercase — but NO special character
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/account/register", new
        {
            Email = $"ldg002c_{Guid.NewGuid():N}@test.com",
            Password = "Password1234"  // 12 chars, upper+lower+digit, no special
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // NFR-SEC-011
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Register_NoDigitPassword_Returns400_LDG_002()
    {
        // 10+ chars, uppercase, special, lowercase — but NO digit
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/account/register", new
        {
            Email = $"ldg002d_{Guid.NewGuid():N}@test.com",
            Password = "Password!!"   // 10 chars, upper+lower+special, no digit
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // NFR-SEC-011
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Register_CommonWeakPassword_Returns400_LDG_002()
    {
        // "password1" — 9 chars, no uppercase, no special
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/account/register", new
        {
            Email = $"ldg002e_{Guid.NewGuid():N}@test.com",
            Password = "password1"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Compliant password accepted ───────────────────────────────────────────

    // NFR-SEC-011
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Register_CompliantPassword_Returns200_LDG_002()
    {
        // "Password1!" — 10 chars, uppercase P, lowercase assword, digit 1, special !
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/account/register", new
        {
            Email = $"ldg002ok_{Guid.NewGuid():N}@test.com",
            Password = "Password1!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("data", out var data));
        Assert.False(string.IsNullOrEmpty(data.GetProperty("id").GetString()));
    }

    // NFR-SEC-011
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Register_StrongPassword_Returns200_LDG_002()
    {
        // Longer strong password passes all four requirements plus length
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/account/register", new
        {
            Email = $"ldg002str_{Guid.NewGuid():N}@test.com",
            Password = "Str0ng!Pass#2026"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // NFR-SEC-011
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Register_WeakPassword_ResponseContainsValidationErrors_LDG_002()
    {
        // Verify the error envelope structure for a weak password
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/account/register", new
        {
            Email = $"ldg002err_{Guid.NewGuid():N}@test.com",
            Password = "weak"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        // Should have error envelope, not data
        Assert.True(body.TryGetProperty("error", out _),
            "400 response should contain 'error' property in envelope.");
    }

    // NFR-SEC-011
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Register_TenCharExactlyMeetingAllRules_Returns200_LDG_002()
    {
        // Boundary case: exactly 10 characters, all four char class rules satisfied
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/account/register", new
        {
            Email = $"ldg002b10_{Guid.NewGuid():N}@test.com",
            Password = "Abcdefg1!Z"   // exactly 10: upper=A,Z; lower=bcdefg; digit=1; special=!
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
