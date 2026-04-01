using Moq;
using TaskPilot.Entities;
using TaskPilot.Repositories.Interfaces;
using TaskPilot.Services;

namespace TaskPilot.Tests.Unit.Services;

public class TaskTypeServiceTests
{
    private readonly Mock<ITaskTypeRepository> _repoMock;
    private readonly TaskTypeService _service;

    public TaskTypeServiceTests()
    {
        _repoMock = new Mock<ITaskTypeRepository>();
        _service = new TaskTypeService(_repoMock.Object);
    }

    [Fact]
    public async Task GetAllActiveAsync_ReturnsAllActiveTypes_OrderedBySortOrder()
    {
        var types = new List<TaskType>
        {
            new() { Id = 1, Name = "Task",    SortOrder = 1, IsActive = true },
            new() { Id = 2, Name = "Inactive",SortOrder = 2, IsActive = false },
            new() { Id = 3, Name = "Goal",    SortOrder = 3, IsActive = true },
            new() { Id = 4, Name = "Habit",   SortOrder = 4, IsActive = true }
        };

        // Repository already filters active + orders — simulate that contract
        var activeOrdered = types
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder)
            .ToList();

        _repoMock.Setup(r => r.GetAllActiveAsync(default))
            .ReturnsAsync(activeOrdered);

        var result = await _service.GetAllActiveAsync();

        Assert.Equal(3, result.Count);
        Assert.Equal("Task", result[0].Name);
        Assert.Equal("Goal", result[1].Name);
        Assert.Equal("Habit", result[2].Name);
        Assert.All(result, r => Assert.True(r.SortOrder > 0));
    }

    [Fact]
    public async Task GetAllActiveAsync_EmptyRepository_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.GetAllActiveAsync(default))
            .ReturnsAsync(new List<TaskType>());

        var result = await _service.GetAllActiveAsync();

        Assert.Empty(result);
    }
}
