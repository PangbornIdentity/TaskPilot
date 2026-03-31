using Moq;
using TaskPilot.Entities;
using TaskPilot.Repositories.Interfaces;
using TaskPilot.Services;
using TaskPilot.Models.Tags;

namespace TaskPilot.Tests.Unit.Services;

public class TagServiceTests
{
    private readonly Mock<ITagRepository> _tagRepoMock;
    private readonly TagService _service;

    public TagServiceTests()
    {
        _tagRepoMock = new Mock<ITagRepository>();
        _service = new TagService(_tagRepoMock.Object);
    }

    private static Tag MakeTag(string userId = "user1") => new()
    {
        Id = Guid.NewGuid(),
        Name = "Work",
        Color = "#6255EC",
        UserId = userId,
        LastModifiedBy = "user:test@example.com"
    };

    [Fact]
    public async Task GetAllTagsAsync_ReturnsAllUserTags()
    {
        var tags = new List<Tag> { MakeTag("user1"), MakeTag("user1") };
        _tagRepoMock.Setup(r => r.GetAllForUserAsync("user1", default)).ReturnsAsync(tags);

        var result = await _service.GetAllTagsAsync("user1");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task CreateTagAsync_ValidRequest_ReturnsTagResponse()
    {
        var request = new CreateTagRequest("Important", "#ff5722");
        _tagRepoMock.Setup(r => r.GetByNameAsync("Important", "user1", default))
            .ReturnsAsync((Tag?)null);
        _tagRepoMock.Setup(r => r.AddAsync(It.IsAny<Tag>(), default)).Returns(Task.CompletedTask);
        _tagRepoMock.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _service.CreateTagAsync(request, "user1", "user:test@example.com");

        Assert.NotNull(result);
        Assert.Equal("Important", result.Name);
        Assert.Equal("#ff5722", result.Color);
    }

    [Fact]
    public async Task CreateTagAsync_DuplicateName_ThrowsInvalidOperationException()
    {
        var existing = MakeTag("user1");
        existing.Name = "Work";
        _tagRepoMock.Setup(r => r.GetByNameAsync("Work", "user1", default)).ReturnsAsync(existing);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateTagAsync(new CreateTagRequest("Work", "#000000"), "user1", "user:test@example.com"));
    }

    [Fact]
    public async Task DeleteTagAsync_ExistingTag_ReturnsTrue()
    {
        var tag = MakeTag("user1");
        _tagRepoMock.Setup(r => r.GetByIdAsync(tag.Id, default)).ReturnsAsync(tag);
        _tagRepoMock.Setup(r => r.Remove(tag));
        _tagRepoMock.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _service.DeleteTagAsync(tag.Id, "user1");

        Assert.True(result);
        _tagRepoMock.Verify(r => r.Remove(tag), Times.Once);
    }

    [Fact]
    public async Task DeleteTagAsync_WrongUser_ReturnsFalse()
    {
        var tag = MakeTag("user1");
        _tagRepoMock.Setup(r => r.GetByIdAsync(tag.Id, default)).ReturnsAsync(tag);

        var result = await _service.DeleteTagAsync(tag.Id, "other-user");

        Assert.False(result);
        _tagRepoMock.Verify(r => r.Remove(It.IsAny<Tag>()), Times.Never);
    }
}
