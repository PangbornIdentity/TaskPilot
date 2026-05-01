using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Moq;
using TaskPilot.Constants;
using TaskPilot.Mcp;
using TaskPilot.Models.Common;
using TaskPilot.Models.Enums;
using TaskPilot.Models.Stats;
using TaskPilot.Models.Tags;
using TaskPilot.Models.Tasks;
using TaskPilot.Models.TaskTypes;
using TaskPilot.Services.Interfaces;
using TaskStatus = TaskPilot.Models.Enums.TaskStatus;

namespace TaskPilot.Tests.Unit.Mcp;

public class TaskPilotMcpToolsTests
{
    private readonly Mock<ITaskService> _taskSvc = new();
    private readonly Mock<ITagService> _tagSvc = new();
    private readonly Mock<ITaskTypeService> _typeSvc = new();
    private readonly Mock<IStatsService> _statsSvc = new();
    private readonly Mock<IHttpContextAccessor> _httpCtx = new();

    private const string UserId = "user-123";
    private const string KeyName = "my-key";

    private TaskPilotMcpTools CreateSut()
    {
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, UserId),
            new Claim(AuthConstants.ApiKeyClaimType, KeyName)
        ], AuthConstants.ApiKeyScheme));
        _httpCtx.Setup(a => a.HttpContext).Returns(ctx);
        return new TaskPilotMcpTools(_taskSvc.Object, _tagSvc.Object, _typeSvc.Object, _statsSvc.Object, _httpCtx.Object);
    }

    private static TaskResponse MakeTaskResponse(Guid? id = null) => new(
        id ?? Guid.NewGuid(), "Test Task", null, 1, "Task", Area.Personal, "Personal",
        TaskPriority.Medium, TaskStatus.NotStarted, TargetDateType.ThisWeek,
        null, null, null, false, null, 1,
        DateTime.UtcNow, DateTime.UtcNow, "api:my-key", UserId, []);

    // MCP-001
    [Fact]
    public async Task ListTasksAsync_NoFilters_ReturnsSerializedPagedResult()
    {
        var task = MakeTaskResponse();
        var paged = new PagedApiResponse<TaskResponse>(
            [task],
            new PagedResponseMeta(DateTime.UtcNow, "req-1", 1, 20, 1, 1));
        _taskSvc.Setup(s => s.GetTasksAsync(It.IsAny<TaskQueryParams>(), UserId, default))
                .ReturnsAsync(paged);

        var result = await CreateSut().ListTasksAsync(cancellationToken: default);

        var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("Data", out _));
    }

    // MCP-002
    [Fact]
    public async Task ListTasksAsync_WithStatusFilter_ParsesEnumCorrectly()
    {
        _taskSvc.Setup(s => s.GetTasksAsync(It.IsAny<TaskQueryParams>(), UserId, default))
                .ReturnsAsync(new PagedApiResponse<TaskResponse>([], new PagedResponseMeta(DateTime.UtcNow, "r", 1, 20, 0, 0)));

        await CreateSut().ListTasksAsync(status: "InProgress");

        _taskSvc.Verify(s => s.GetTasksAsync(
            It.Is<TaskQueryParams>(q => q.Status == TaskStatus.InProgress),
            UserId, default), Times.Once);
    }

    // MCP-003
    [Fact]
    public async Task ListTasksAsync_InvalidStatusString_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateSut().ListTasksAsync(status: "Bogus"));
    }

    // MCP-004
    [Fact]
    public async Task GetTaskAsync_TaskExists_ReturnsSerializedTask()
    {
        var id = Guid.NewGuid();
        var task = MakeTaskResponse(id);
        _taskSvc.Setup(s => s.GetTaskByIdAsync(id, UserId, default)).ReturnsAsync(task);

        var result = await CreateSut().GetTaskAsync(id.ToString());

        var doc = JsonDocument.Parse(result);
        Assert.Equal(id.ToString(), doc.RootElement.GetProperty("Id").GetString());
    }

    // MCP-005
    [Fact]
    public async Task GetTaskAsync_TaskNotFound_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        _taskSvc.Setup(s => s.GetTaskByIdAsync(id, UserId, default)).ReturnsAsync((TaskResponse?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut().GetTaskAsync(id.ToString()));
    }

    // MCP-006
    [Fact]
    public async Task CreateTaskAsync_ValidRequest_ReturnsSerializedTask()
    {
        var task = MakeTaskResponse();
        _taskSvc.Setup(s => s.CreateTaskAsync(It.IsAny<CreateTaskRequest>(), UserId, It.IsAny<string>(), default))
                .ReturnsAsync(task);

        var result = await CreateSut().CreateTaskAsync(
            "My Task", 1, "Personal", "Medium", "NotStarted", "ThisWeek");

        Assert.NotEmpty(result);
        _taskSvc.Verify(s => s.CreateTaskAsync(It.IsAny<CreateTaskRequest>(), UserId, It.IsAny<string>(), default), Times.Once);
    }

    // MCP-007
    [Fact]
    public async Task CreateTaskAsync_ModifiedByUsesApiKeyName()
    {
        _taskSvc.Setup(s => s.CreateTaskAsync(It.IsAny<CreateTaskRequest>(), UserId, It.IsAny<string>(), default))
                .ReturnsAsync(MakeTaskResponse());

        await CreateSut().CreateTaskAsync("Task", 1, "Personal", "Medium", "NotStarted", "ThisWeek");

        _taskSvc.Verify(s => s.CreateTaskAsync(
            It.IsAny<CreateTaskRequest>(), UserId, $"api:{KeyName}", default), Times.Once);
    }

    // MCP-008
    [Fact]
    public async Task UpdateTaskAsync_TaskNotFound_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        _taskSvc.Setup(s => s.PatchTaskAsync(id, It.IsAny<PatchTaskRequest>(), UserId, It.IsAny<string>(), default))
                .ReturnsAsync((TaskResponse?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut().UpdateTaskAsync(id.ToString(), title: "New"));
    }

    // MCP-009
    [Fact]
    public async Task CompleteTaskAsync_TaskExists_ReturnsSerializedTask()
    {
        var id = Guid.NewGuid();
        var task = MakeTaskResponse(id);
        _taskSvc.Setup(s => s.CompleteTaskAsync(id, It.IsAny<CompleteTaskRequest>(), UserId, It.IsAny<string>(), default))
                .ReturnsAsync(task);

        var result = await CreateSut().CompleteTaskAsync(id.ToString(), "Great work");

        _taskSvc.Verify(s => s.CompleteTaskAsync(
            id,
            It.Is<CompleteTaskRequest>(r => r.ResultAnalysis == "Great work"),
            UserId, It.IsAny<string>(), default), Times.Once);
        Assert.NotEmpty(result);
    }

    // MCP-010
    [Fact]
    public async Task DeleteTaskAsync_TaskDeleted_ReturnsSuccessJson()
    {
        var id = Guid.NewGuid();
        _taskSvc.Setup(s => s.DeleteTaskAsync(id, UserId, It.IsAny<string>(), default)).ReturnsAsync(true);

        var result = await CreateSut().DeleteTaskAsync(id.ToString());

        var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
    }

    // MCP-011
    [Fact]
    public async Task DeleteTaskAsync_TaskNotFound_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        _taskSvc.Setup(s => s.DeleteTaskAsync(id, UserId, It.IsAny<string>(), default)).ReturnsAsync(false);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut().DeleteTaskAsync(id.ToString()));
    }

    // MCP-012
    [Fact]
    public async Task GetStatsAsync_ReturnsSerializedStats()
    {
        var stats = new TaskStatsResponse(5, 2, 1, 1, 0, [], [], [], [], [], [], [], new(0, 0), [], new(0, 0, 0, 0));
        _statsSvc.Setup(s => s.GetTaskStatsAsync(UserId, default)).ReturnsAsync(stats);

        var result = await CreateSut().GetStatsAsync();

        Assert.NotEmpty(result);
        _statsSvc.Verify(s => s.GetTaskStatsAsync(UserId, default), Times.Once);
    }

    // MCP-013
    [Fact]
    public async Task ListTagsAsync_ReturnsSerializedTagList()
    {
        var tags = new List<TagResponse> { new(Guid.NewGuid(), "Work", "#ff0000", DateTime.UtcNow) };
        _tagSvc.Setup(s => s.GetAllTagsAsync(UserId, default)).ReturnsAsync(tags);

        var result = await CreateSut().ListTagsAsync();

        var doc = JsonDocument.Parse(result);
        Assert.Equal(1, doc.RootElement.GetArrayLength());
    }

    // MCP-014
    [Fact]
    public async Task ListTaskTypesAsync_ReturnsSerializedTaskTypeList()
    {
        var types = new List<TaskTypeResponse> { new(1, "Task", 1) };
        _typeSvc.Setup(s => s.GetAllActiveAsync(default)).ReturnsAsync(types);

        var result = await CreateSut().ListTaskTypesAsync();

        var doc = JsonDocument.Parse(result);
        Assert.Equal(1, doc.RootElement.GetArrayLength());
        _typeSvc.Verify(s => s.GetAllActiveAsync(default), Times.Once);
    }
}
