using Moq;
using TaskPilot.Entities;
using TaskPilot.Repositories.Interfaces;
using TaskPilot.Services;
using TaskPilot.Models.Audit;

namespace TaskPilot.Tests.Unit.Services;

public class AuditServiceTests
{
    private readonly Mock<IAuditLogRepository> _repoMock;
    private readonly AuditService _service;

    public AuditServiceTests()
    {
        _repoMock = new Mock<IAuditLogRepository>();
        _service = new AuditService(_repoMock.Object);
    }

    private static ApiAuditLog MakeLog(string userId = "user1") => new()
    {
        Id = Guid.NewGuid(),
        ApiKeyId = Guid.NewGuid(),
        ApiKeyName = "TestKey",
        Timestamp = DateTime.UtcNow,
        HttpMethod = "GET",
        Endpoint = "/api/v1/tasks",
        RequestBodyHash = "abc123",
        ResponseStatusCode = 200,
        DurationMs = 42,
        UserId = userId
    };

    [Fact]
    public async Task GetAuditLogsAsync_ReturnsMappedResponses()
    {
        var log = MakeLog();
        var queryParams = new AuditQueryParams(null, null, null, null, null, null, 1, 10);

        _repoMock.Setup(r => r.GetPagedAsync(queryParams, "user1", default))
            .ReturnsAsync((new List<ApiAuditLog> { log }, 1));

        var result = await _service.GetAuditLogsAsync(queryParams, "user1");

        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal(log.Id, result.Data[0].Id);
        Assert.Equal("GET", result.Data[0].HttpMethod);
        Assert.Equal("/api/v1/tasks", result.Data[0].Endpoint);
        Assert.Equal(200, result.Data[0].ResponseStatusCode);
    }

    [Fact]
    public async Task GetAuditLogsAsync_EmptyResult_ReturnsEmptyData()
    {
        var queryParams = new AuditQueryParams(null, null, null, null, null, null, 1, 10);

        _repoMock.Setup(r => r.GetPagedAsync(queryParams, "user1", default))
            .ReturnsAsync((new List<ApiAuditLog>(), 0));

        var result = await _service.GetAuditLogsAsync(queryParams, "user1");

        Assert.Empty(result.Data);
        Assert.Equal(0, result.Meta.TotalCount);
    }

    [Fact]
    public async Task GetAuditLogsAsync_CalculatesPageCount()
    {
        var logs = Enumerable.Range(0, 5).Select(_ => MakeLog()).ToList();
        var queryParams = new AuditQueryParams(null, null, null, null, null, null, 1, 3);

        _repoMock.Setup(r => r.GetPagedAsync(queryParams, "user1", default))
            .ReturnsAsync((logs.Take(3).ToList(), 5));

        var result = await _service.GetAuditLogsAsync(queryParams, "user1");

        Assert.Equal(5, result.Meta.TotalCount);
        Assert.Equal(2, result.Meta.TotalPages); // ceil(5/3)
    }

    [Fact]
    public async Task GetAuditLogsAsync_MapsDurationMs()
    {
        var log = MakeLog();
        log.DurationMs = 150;
        var queryParams = new AuditQueryParams(null, null, null, null, null, null, 1, 10);

        _repoMock.Setup(r => r.GetPagedAsync(queryParams, "user1", default))
            .ReturnsAsync((new List<ApiAuditLog> { log }, 1));

        var result = await _service.GetAuditLogsAsync(queryParams, "user1");

        Assert.Equal(150, result.Data[0].DurationMs);
    }

    [Fact]
    public async Task GetSummaryAsync_DelegatesToRepository()
    {
        var summary = new AuditSummaryResponse(42, 5, 3, 2);

        _repoMock.Setup(r => r.GetSummaryAsync("user1", default)).ReturnsAsync(summary);

        var result = await _service.GetSummaryAsync("user1");

        Assert.Equal(42, result.TotalRequests);
        Assert.Equal(5, result.GetsToday);
        Assert.Equal(3, result.WritesToday);
        Assert.Equal(2, result.ActiveApiKeys);
    }
}
