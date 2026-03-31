using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TaskPilot.Tests.Integration.Helpers;

namespace TaskPilot.Tests.Integration.Audit;

[Collection("Integration")]
public class AuditApiTests : IClassFixture<TaskPilotWebAppFactory>
{
    private readonly TaskPilotWebAppFactory _factory;

    public AuditApiTests(TaskPilotWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAuditLogs_AuthenticatedUser_Returns200WithPagedResponse()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync("/api/v1/audit");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("data", out _));
        Assert.True(body.TryGetProperty("meta", out _));
    }

    [Fact]
    public async Task GetAuditLogs_UnauthenticatedUser_Returns401()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/v1/audit");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_ReturnsEmptyDataForNewUser()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync("/api/v1/audit");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");
        Assert.Equal(JsonValueKind.Array, data.ValueKind);
        // New user with cookie auth has no API key audit logs
        Assert.Equal(0, data.GetArrayLength());
    }

    [Fact]
    public async Task GetAuditLogs_WithPaginationParams_Returns200()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync("/api/v1/audit?page=1&pageSize=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var meta = body.GetProperty("meta");
        Assert.Equal(1, meta.GetProperty("page").GetInt32());
        Assert.Equal(5, meta.GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task GetAuditSummary_AuthenticatedUser_Returns200WithSummary()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync("/api/v1/audit/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");
        Assert.True(data.TryGetProperty("totalRequests", out _));
        Assert.True(data.TryGetProperty("getsToday", out _));
        Assert.True(data.TryGetProperty("writesToday", out _));
        Assert.True(data.TryGetProperty("activeApiKeys", out _));
    }

    [Fact]
    public async Task GetAuditSummary_UnauthenticatedUser_Returns401()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/v1/audit/summary");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditSummary_NewUser_ReturnsZeroCounts()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync("/api/v1/audit/summary");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");
        Assert.Equal(0, data.GetProperty("totalRequests").GetInt32());
        Assert.Equal(0, data.GetProperty("activeApiKeys").GetInt32());
    }
}
