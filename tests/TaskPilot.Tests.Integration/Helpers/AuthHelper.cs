using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TaskPilot.Tests.Integration.Helpers;

public static class AuthHelper
{
    public static async Task<(HttpClient Client, string UserId)> CreateAuthenticatedClientAsync(
        TaskPilotWebAppFactory factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var email = $"test_{Guid.NewGuid():N}@example.com";
        const string password = "TestPass123!";

        var response = await client.PostAsJsonAsync("/api/v1/account/register", new
        {
            Email = email,
            Password = password
        });

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var userId = body.GetProperty("data").GetProperty("id").GetString()!;

        return (client, userId);
    }
}
