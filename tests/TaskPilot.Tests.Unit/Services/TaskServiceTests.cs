using Moq;
using TaskPilot.Server.Entities;
using TaskPilot.Server.Repositories.Interfaces;
using TaskPilot.Server.Services;
using TaskPilot.Shared.DTOs.Tasks;
using TaskPilot.Shared.Enums;

namespace TaskPilot.Tests.Unit.Services;

public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _taskRepoMock;
    private readonly Mock<ITagRepository> _tagRepoMock;
    private readonly TaskService _service;

    public TaskServiceTests()
    {
        _taskRepoMock = new Mock<ITaskRepository>();
        _tagRepoMock = new Mock<ITagRepository>();
        _service = new TaskService(_taskRepoMock.Object, _tagRepoMock.Object);
    }

    private static TaskItem MakeTask(string userId = "user1") => new()
    {
        Id = Guid.NewGuid(),
        Title = "Test Task",
        Type = "Work",
        Priority = TaskPriority.Medium,
        Status = Shared.Enums.TaskStatus.NotStarted,
        TargetDateType = TargetDateType.ThisWeek,
        SortOrder = 1,
        UserId = userId,
        LastModifiedBy = "user:test@example.com",
        TaskTags = [],
        ActivityLogs = []
    };

    [Fact]
    public async Task CreateTaskAsync_ValidRequest_ReturnsTaskResponse()
    {
        var request = new CreateTaskRequest("My Task", null, "Work", TaskPriority.Medium,
            Shared.Enums.TaskStatus.NotStarted, TargetDateType.ThisWeek, null, false, null, null);

        _taskRepoMock.Setup(r => r.GetMaxSortOrderAsync("user1", default)).ReturnsAsync(0);
        _taskRepoMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>(), default)).Returns(Task.CompletedTask);
        _taskRepoMock.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _service.CreateTaskAsync(request, "user1", "user:test@example.com");

        Assert.NotNull(result);
        Assert.Equal("My Task", result.Title);
        Assert.Equal("user1", result.UserId);
    }

    [Fact]
    public async Task CreateTaskAsync_WithTags_TagsAttachedToTask()
    {
        var tagId = Guid.NewGuid();
        var tags = new List<Tag> { new() { Id = tagId, Name = "Important", Color = "#ff0000", UserId = "user1" } };
        var request = new CreateTaskRequest("Task with tag", null, "Work", TaskPriority.High,
            Shared.Enums.TaskStatus.NotStarted, TargetDateType.ThisWeek, null, false, null, [tagId]);

        _taskRepoMock.Setup(r => r.GetMaxSortOrderAsync("user1", default)).ReturnsAsync(0);
        _tagRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), "user1", default))
            .ReturnsAsync(tags);
        _taskRepoMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>(), default)).Returns(Task.CompletedTask);
        _taskRepoMock.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        TaskItem? capturedTask = null;
        _taskRepoMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>(), default))
            .Callback<TaskItem, CancellationToken>((t, _) =>
            {
                capturedTask = t;
                // Populate navigation property so MapToResponse works
                foreach (var tt in t.TaskTags)
                    tt.Tag = tags.First(tag => tag.Id == tt.TagId);
            })
            .Returns(Task.CompletedTask);

        await _service.CreateTaskAsync(request, "user1", "user:test@example.com");

        Assert.NotNull(capturedTask);
        Assert.Single(capturedTask.TaskTags);
        Assert.Equal(tagId, capturedTask.TaskTags.First().TagId);
    }

    [Fact]
    public async Task CreateTaskAsync_SetsLastModifiedBy()
    {
        var request = new CreateTaskRequest("Task", null, "Personal", TaskPriority.Low,
            Shared.Enums.TaskStatus.NotStarted, TargetDateType.ThisWeek, null, false, null, null);

        _taskRepoMock.Setup(r => r.GetMaxSortOrderAsync("user1", default)).ReturnsAsync(0);
        _taskRepoMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>(), default)).Returns(Task.CompletedTask);
        _taskRepoMock.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        TaskItem? capturedTask = null;
        _taskRepoMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>(), default))
            .Callback<TaskItem, CancellationToken>((t, _) => capturedTask = t)
            .Returns(Task.CompletedTask);

        await _service.CreateTaskAsync(request, "user1", "user:john@example.com");

        Assert.Equal("user:john@example.com", capturedTask?.LastModifiedBy);
    }

    [Fact]
    public async Task GetTaskByIdAsync_ExistingTask_ReturnsResponse()
    {
        var id = Guid.NewGuid();
        var task = MakeTask();
        task.Id = id;

        _taskRepoMock.Setup(r => r.GetByIdWithDetailsAsync(id, "user1", default)).ReturnsAsync(task);

        var result = await _service.GetTaskByIdAsync(id, "user1");

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("Test Task", result.Title);
    }

    [Fact]
    public async Task GetTaskByIdAsync_NotFound_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _taskRepoMock.Setup(r => r.GetByIdWithDetailsAsync(id, "user1", default))
            .ReturnsAsync((TaskItem?)null);

        var result = await _service.GetTaskByIdAsync(id, "user1");

        Assert.Null(result);
    }

    [Fact]
    public async Task CompleteTaskAsync_SetsStatusAndCompletedDate()
    {
        var task = MakeTask();
        var request = new CompleteTaskRequest(null);

        _taskRepoMock.Setup(r => r.GetByIdWithTagsAsync(task.Id, "user1", default)).ReturnsAsync(task);
        _taskRepoMock.Setup(r => r.Update(task));
        _taskRepoMock.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _service.CompleteTaskAsync(task.Id, request, "user1", "user:test@example.com");

        Assert.NotNull(result);
        Assert.Equal(Shared.Enums.TaskStatus.Completed, result.Status);
        Assert.NotNull(result.CompletedDate);
    }

    [Fact]
    public async Task CompleteTaskAsync_WithResultAnalysis_SetsResultAnalysis()
    {
        var task = MakeTask();
        var request = new CompleteTaskRequest("Great outcome!");

        _taskRepoMock.Setup(r => r.GetByIdWithTagsAsync(task.Id, "user1", default)).ReturnsAsync(task);
        _taskRepoMock.Setup(r => r.Update(task));
        _taskRepoMock.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _service.CompleteTaskAsync(task.Id, request, "user1", "user:test@example.com");

        Assert.NotNull(result);
        Assert.Equal("Great outcome!", result.ResultAnalysis);
    }

    [Fact]
    public async Task CompleteTaskAsync_RecurringTask_CreatesSuccessor()
    {
        var task = MakeTask();
        task.IsRecurring = true;
        task.RecurrencePattern = RecurrencePattern.Daily;
        task.TargetDate = DateTime.UtcNow;
        var request = new CompleteTaskRequest(null);

        _taskRepoMock.Setup(r => r.GetByIdWithTagsAsync(task.Id, "user1", default)).ReturnsAsync(task);
        _taskRepoMock.Setup(r => r.GetMaxSortOrderAsync("user1", default)).ReturnsAsync(1);
        _taskRepoMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>(), default)).Returns(Task.CompletedTask);
        _taskRepoMock.Setup(r => r.Update(task));
        _taskRepoMock.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        await _service.CompleteTaskAsync(task.Id, request, "user1", "user:test@example.com");

        _taskRepoMock.Verify(r => r.AddAsync(It.IsAny<TaskItem>(), default), Times.Once);
    }

    [Fact]
    public async Task DeleteTaskAsync_ExistingTask_SetsIsDeletedAndDeletedAt()
    {
        var task = MakeTask("user1");

        _taskRepoMock.Setup(r => r.GetByIdAsync(task.Id, default)).ReturnsAsync(task);
        _taskRepoMock.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _service.DeleteTaskAsync(task.Id, "user1", "user:test@example.com");

        // Verify the service mutated the task object (EF change tracking picks this up)
        Assert.True(result);
        Assert.True(task.IsDeleted);
        Assert.NotNull(task.DeletedAt);
    }

    [Fact]
    public async Task DeleteTaskAsync_WrongUser_ReturnsFalse()
    {
        var task = MakeTask("user1");

        _taskRepoMock.Setup(r => r.GetByIdAsync(task.Id, default)).ReturnsAsync(task);

        var result = await _service.DeleteTaskAsync(task.Id, "other-user", "user:other@example.com");

        Assert.False(result);
        _taskRepoMock.Verify(r => r.Update(It.IsAny<TaskItem>()), Times.Never);
    }

    [Fact]
    public async Task PatchTaskAsync_NullFields_DoesNotOverwrite()
    {
        var task = MakeTask("user1");
        task.Title = "Original Title";
        task.Priority = TaskPriority.High;

        _taskRepoMock.Setup(r => r.GetByIdWithTagsAsync(task.Id, "user1", default)).ReturnsAsync(task);
        _taskRepoMock.Setup(r => r.Update(task));
        _taskRepoMock.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        // Only patch priority — Title should remain unchanged
        var request = new PatchTaskRequest(Priority: TaskPriority.Low);

        var result = await _service.PatchTaskAsync(task.Id, request, "user1", "user:test@example.com");

        Assert.NotNull(result);
        Assert.Equal("Original Title", result.Title);
        Assert.Equal(TaskPriority.Low, result.Priority);
    }

    [Fact]
    public async Task UpdateTaskAsync_ChangedFields_CreatesActivityLogs()
    {
        var task = MakeTask("user1");
        task.Title = "Old Title";

        _taskRepoMock.Setup(r => r.GetByIdWithTagsAsync(task.Id, "user1", default)).ReturnsAsync(task);
        _tagRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), "user1", default))
            .ReturnsAsync([]);
        _taskRepoMock.Setup(r => r.Update(task));
        _taskRepoMock.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        var request = new UpdateTaskRequest("New Title", null, "Work", TaskPriority.Medium,
            Shared.Enums.TaskStatus.NotStarted, TargetDateType.ThisWeek, null, false, null, null);

        await _service.UpdateTaskAsync(task.Id, request, "user1", "user:test@example.com");

        // The Title changed so an activity log should have been added
        Assert.Contains(task.ActivityLogs, log => log.FieldChanged == "Title");
    }
}
