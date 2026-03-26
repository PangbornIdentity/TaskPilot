using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TaskPilot.Tests.Integration.Helpers;

namespace TaskPilot.Tests.Integration.Auth;

[Collection("Integration")]
public class AuthApiTests : IClassFixture<TaskPilotWebAppFactory>
{
    private readonly TaskPilotWebAppFactory _factory;

    public AuthApiTests(TaskPilotWebAppFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true
    });

    [Fact]
    public async Task Register_ValidCredentials_Returns200WithUserId()
    {
        var client = CreateClient();
        var email = $"newuser_{Guid.NewGuid():N}@example.com";

        var response = await client.PostAsJsonAsync("/api/v1/account/register", new
        {
            Email = email,
            Password = "TestPass123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("data", out var data));
        Assert.False(string.IsNullOrEmpty(data.GetProperty("id").GetString()));
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        var client = CreateClient();
        var email = $"dup_{Guid.NewGuid():N}@example.com";
        var request = new { Email = email, Password = "TestPass123!" };

        await client.PostAsJsonAsync("/api/v1/account/register", request);
        var response = await client.PostAsJsonAsync("/api/v1/account/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_SetsCookieAndReturnsUser()
    {
        var client = CreateClient();
        var email = $"login_{Guid.NewGuid():N}@example.com";
        const string password = "TestPass123!";

        await client.PostAsJsonAsync("/api/v1/account/register", new { Email = email, Password = password });

        // Create fresh client to test login
        var loginClient = CreateClient();
        var response = await loginClient.PostAsJsonAsync("/api/v1/account/login", new
        {
            Email = email,
            Password = password
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(email, body.GetProperty("data").GetProperty("email").GetString());
    }

    [Fact]
    public async Task Login_InvalidPassword_Returns401()
    {
        var client = CreateClient();
        var email = $"badpwd_{Guid.NewGuid():N}@example.com";

        await client.PostAsJsonAsync("/api/v1/account/register", new { Email = email, Password = "TestPass123!" });

        var response = await client.PostAsJsonAsync("/api/v1/account/login", new
        {
            Email = email,
            Password = "WrongPassword!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_AuthenticatedUser_Returns204()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.PostAsync("/api/v1/account/logout", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_Authenticated_ReturnsCurrentUser()
    {
        var (client, userId) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync("/api/v1/account/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(userId, body.GetProperty("data").GetProperty("id").GetString());
    }

    [Fact]
    public async Task GetMe_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var response = await client.GetAsync("/api/v1/account/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ApiKey_ValidKey_AuthenticatesRequest()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        // Generate an API key
        var genResponse = await client.PostAsJsonAsync("/api/v1/apikeys",
            new { Name = "Test Integration Key" });
        genResponse.EnsureSuccessStatusCode();
        var genBody = await genResponse.Content.ReadFromJsonAsync<JsonElement>();
        var plainTextKey = genBody.GetProperty("data").GetProperty("plainTextKey").GetString()!;

        // Create a new client without cookies, use API key header
        var apiClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
        apiClient.DefaultRequestHeaders.Add("X-Api-Key", plainTextKey);

        var response = await apiClient.GetAsync("/api/v1/tasks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ApiKey_InvalidKey_Returns401()
    {
        var apiClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
        apiClient.DefaultRequestHeaders.Add("X-Api-Key", "tp-totally-invalid-key");

        var response = await apiClient.GetAsync("/api/v1/tasks");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ApiKey_InactiveKey_Returns401()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        // Generate a key then deactivate it
        var genResponse = await client.PostAsJsonAsync("/api/v1/apikeys",
            new { Name = "Key to Deactivate" });
        genResponse.EnsureSuccessStatusCode();
        var genBody = await genResponse.Content.ReadFromJsonAsync<JsonElement>();
        var plainTextKey = genBody.GetProperty("data").GetProperty("plainTextKey").GetString()!;
        var keyId = genBody.GetProperty("data").GetProperty("id").GetString()!;

        // Deactivate
        await client.PostAsync($"/api/v1/apikeys/{keyId}/deactivate", null);

        // Try to use deactivated key
        var apiClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
        apiClient.DefaultRequestHeaders.Add("X-Api-Key", plainTextKey);

        var response = await apiClient.GetAsync("/api/v1/tasks");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
