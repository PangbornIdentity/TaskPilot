using System.Net;
using TaskPilot.Tests.Integration.Helpers;

namespace TaskPilot.Tests.Integration.Changelog;

[Collection("Integration")]
public class ChangelogPageTests : IClassFixture<TaskPilotWebAppFactory>
{
    private readonly TaskPilotWebAppFactory _factory;

    public ChangelogPageTests(TaskPilotWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ChangelogPage_UnauthenticatedUser_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/changelog");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/auth/login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task ChangelogPage_AuthenticatedUser_Returns200()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync("/changelog");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ChangelogPage_AuthenticatedUser_ShowsVersionEntries()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync("/changelog");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("v1.", content);
        Assert.Contains("What's new", content);
    }

    [Fact]
    public async Task ChangelogPage_AuthenticatedUser_ShowsChangeTypes()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync("/changelog");
        var content = await response.Content.ReadAsStringAsync();

        Assert.True(
            content.Contains("Feature") || content.Contains("Fix") || content.Contains("Improvement"),
            "Expected at least one change type badge");
    }
}
