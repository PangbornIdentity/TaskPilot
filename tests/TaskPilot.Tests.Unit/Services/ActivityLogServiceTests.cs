using Moq;
using TaskPilot.Models.Audit;
using TaskPilot.Models.Common;
using TaskPilot.Repositories.Interfaces;
using TaskPilot.Services;

namespace TaskPilot.Tests.Unit.Services;

public class ActivityLogServiceTests
{
    private readonly Mock<IActivityLogRepository> _repoMock;
    private readonly ActivityLogService _service;

    public ActivityLogServiceTests()
    {
        _repoMock = new Mock<IActivityLogRepository>();
        _service = new ActivityLogService(_repoMock.Object);
    }

    private static ActivityLogResponse MakeLog(
        string taskTitle = "Test Task",
        string field = "Status",
        string? oldVal = "NotStarted",
        string? newVal = "InProgress",
        string changedBy = "user:test@example.com") =>
        new(Guid.NewGuid(), Guid.NewGuid(), taskTitle, DateTime.UtcNow, field, oldVal, newVal, changedBy);

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedResponse()
    {
        var log = MakeLog();
        var queryParams = new ActivityLogQueryParams();

        _repoMock.Setup(r => r.GetPagedAsync(queryParams, "user1", default))
            .ReturnsAsync((new List<ActivityLogResponse> { log }, 1));

        var result = await _service.GetPagedAsync(queryParams, "user1");

        Assert.Single(result.Data);
        Assert.Equal(log.Id, result.Data[0].Id);
        Assert.Equal("Test Task", result.Data[0].TaskTitle);
        Assert.Equal("Status", result.Data[0].FieldChanged);
    }

    [Fact]
    public async Task GetPagedAsync_CalculatesPageCount()
    {
        var logs = Enumerable.Range(0, 3).Select(_ => MakeLog()).ToList();
        var queryParams = new ActivityLogQueryParams(Page: 1, PageSize: 2);

        _repoMock.Setup(r => r.GetPagedAsync(queryParams, "user1", default))
            .ReturnsAsync((logs.Take(2).ToList(), 3));

        var result = await _service.GetPagedAsync(queryParams, "user1");

        Assert.Equal(3, result.Meta.TotalCount);
        Assert.Equal(2, result.Meta.TotalPages); // ceil(3/2)
    }

    [Fact]
    public async Task GetPagedAsync_EmptyResult_ReturnsEmptyData()
    {
        var queryParams = new ActivityLogQueryParams();

        _repoMock.Setup(r => r.GetPagedAsync(queryParams, "user1", default))
            .ReturnsAsync((new List<ActivityLogResponse>(), 0));

        var result = await _service.GetPagedAsync(queryParams, "user1");

        Assert.Empty(result.Data);
        Assert.Equal(0, result.Meta.TotalCount);
        Assert.Equal(1, result.Meta.TotalPages); // clamped to 1 — empty result is still page 1 of 1
    }

    [Fact]
    public async Task GetForTaskAsync_DelegatesToRepository()
    {
        var taskId = Guid.NewGuid();
        var logs = new List<ActivityLogResponse> { MakeLog(), MakeLog(field: "Title") };

        _repoMock.Setup(r => r.GetForTaskAsync(taskId, "user1", default))
            .ReturnsAsync(logs);

        var result = await _service.GetForTaskAsync(taskId, "user1");

        Assert.Equal(2, result.Count);
        _repoMock.Verify(r => r.GetForTaskAsync(taskId, "user1", default), Times.Once);
    }

    [Fact]
    public async Task GetForTaskAsync_EmptyForTaskWithNoHistory()
    {
        var taskId = Guid.NewGuid();

        _repoMock.Setup(r => r.GetForTaskAsync(taskId, "user1", default))
            .ReturnsAsync(new List<ActivityLogResponse>());

        var result = await _service.GetForTaskAsync(taskId, "user1");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPagedAsync_PassesQueryParamsToRepository()
    {
        var taskId = Guid.NewGuid();
        var queryParams = new ActivityLogQueryParams(TaskId: taskId, FieldChanged: "Status");

        _repoMock.Setup(r => r.GetPagedAsync(queryParams, "user1", default))
            .ReturnsAsync((new List<ActivityLogResponse>(), 0));

        await _service.GetPagedAsync(queryParams, "user1");

        _repoMock.Verify(r => r.GetPagedAsync(queryParams, "user1", default), Times.Once);
    }
}
