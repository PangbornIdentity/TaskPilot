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
        var rows = new List<(Tag, int)> { (MakeTag("user1"), 0), (MakeTag("user1"), 0) };
        _tagRepoMock.Setup(r => r.GetAllForUserWithTaskCountAsync("user1", default)).ReturnsAsync(rows);

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

    [Fact]
    public async Task GetAllTagsAsync_PopulatesTaskCountFromRepository()
    {
        var t1 = MakeTag("user1"); t1.Name = "alpha";
        var t2 = MakeTag("user1"); t2.Name = "beta";
        var rows = new List<(Tag, int)> { (t1, 3), (t2, 0) };
        _tagRepoMock.Setup(r => r.GetAllForUserWithTaskCountAsync("user1", default)).ReturnsAsync(rows);

        var result = await _service.GetAllTagsAsync("user1");

        Assert.Equal(3, result.First(r => r.Name == "alpha").TaskCount);
        Assert.Equal(0, result.First(r => r.Name == "beta").TaskCount);
    }

    [Fact]
    public async Task UpdateTagAsync_ValidRequest_UpdatesNameAndColor()
    {
        var tag = MakeTag("user1"); tag.Name = "old"; tag.Color = "#000000";
        _tagRepoMock.Setup(r => r.GetByIdAsync(tag.Id, default)).ReturnsAsync(tag);
        _tagRepoMock.Setup(r => r.GetByNameAsync("new", "user1", default)).ReturnsAsync((Tag?)null);
        _tagRepoMock.Setup(r => r.GetAllForUserWithTaskCountAsync("user1", default))
            .ReturnsAsync(new List<(Tag, int)> { (tag, 2) });
        _tagRepoMock.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _service.UpdateTagAsync(tag.Id,
            new UpdateTagRequest("new", "#ABCDEF"), "user1", "user:x@example.com");

        Assert.NotNull(result);
        Assert.Equal("new", result!.Name);
        Assert.Equal("#ABCDEF", result.Color);
        Assert.Equal(2, result.TaskCount);
        Assert.Equal("new", tag.Name);
        Assert.Equal("#ABCDEF", tag.Color);
        Assert.Equal("user:x@example.com", tag.LastModifiedBy);
    }

    [Fact]
    public async Task UpdateTagAsync_TagDoesNotExist_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _tagRepoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Tag?)null);

        var result = await _service.UpdateTagAsync(id,
            new UpdateTagRequest("name", "#000000"), "user1", "user:x@example.com");

        Assert.Null(result);
        _tagRepoMock.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task UpdateTagAsync_WrongUser_ReturnsNull()
    {
        var tag = MakeTag("owner");
        _tagRepoMock.Setup(r => r.GetByIdAsync(tag.Id, default)).ReturnsAsync(tag);

        var result = await _service.UpdateTagAsync(tag.Id,
            new UpdateTagRequest("renamed", "#000000"), "intruder", "user:bad@example.com");

        Assert.Null(result);
        _tagRepoMock.Verify(r => r.SaveChangesAsync(default), Times.Never);
        // Original entity must remain untouched.
        Assert.Equal("Work", tag.Name);
    }

    [Fact]
    public async Task UpdateTagAsync_DuplicateNameForSameUser_ThrowsInvalidOperationException()
    {
        var tag = MakeTag("user1"); tag.Name = "old";
        var collision = MakeTag("user1"); collision.Name = "taken";
        _tagRepoMock.Setup(r => r.GetByIdAsync(tag.Id, default)).ReturnsAsync(tag);
        _tagRepoMock.Setup(r => r.GetByNameAsync("taken", "user1", default)).ReturnsAsync(collision);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateTagAsync(tag.Id,
                new UpdateTagRequest("taken", "#000000"), "user1", "user:x@example.com"));

        _tagRepoMock.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task UpdateTagAsync_SameNameDifferentUser_DoesNotConflict()
    {
        // The duplicate-name check is scoped to the same user — a tag named
        // "shared" owned by user2 must not block user1 from renaming to "shared".
        var tag = MakeTag("user1"); tag.Name = "old";
        _tagRepoMock.Setup(r => r.GetByIdAsync(tag.Id, default)).ReturnsAsync(tag);
        _tagRepoMock.Setup(r => r.GetByNameAsync("shared", "user1", default)).ReturnsAsync((Tag?)null);
        _tagRepoMock.Setup(r => r.GetAllForUserWithTaskCountAsync("user1", default))
            .ReturnsAsync(new List<(Tag, int)> { (tag, 0) });
        _tagRepoMock.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _service.UpdateTagAsync(tag.Id,
            new UpdateTagRequest("shared", "#111111"), "user1", "user:x@example.com");

        Assert.NotNull(result);
        Assert.Equal("shared", result!.Name);
    }

    [Fact]
    public async Task UpdateTagAsync_OnlyColorChanged_AllowsSameName()
    {
        var tag = MakeTag("user1"); tag.Name = "keepme"; tag.Color = "#000000";
        _tagRepoMock.Setup(r => r.GetByIdAsync(tag.Id, default)).ReturnsAsync(tag);
        // Same name → duplicate check is skipped entirely; mock is not configured to
        // return a value, which would yield a fake collision otherwise.
        _tagRepoMock.Setup(r => r.GetAllForUserWithTaskCountAsync("user1", default))
            .ReturnsAsync(new List<(Tag, int)> { (tag, 1) });
        _tagRepoMock.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _service.UpdateTagAsync(tag.Id,
            new UpdateTagRequest("keepme", "#FF8800"), "user1", "user:x@example.com");

        Assert.NotNull(result);
        Assert.Equal("keepme", result!.Name);
        Assert.Equal("#FF8800", result.Color);
        _tagRepoMock.Verify(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task UpdateTagAsync_SetsLastModifiedByToCaller()
    {
        var tag = MakeTag("user1");
        var originalModifier = tag.LastModifiedBy;
        _tagRepoMock.Setup(r => r.GetByIdAsync(tag.Id, default)).ReturnsAsync(tag);
        _tagRepoMock.Setup(r => r.GetByNameAsync(It.IsAny<string>(), "user1", default)).ReturnsAsync((Tag?)null);
        _tagRepoMock.Setup(r => r.GetAllForUserWithTaskCountAsync("user1", default))
            .ReturnsAsync(new List<(Tag, int)> { (tag, 0) });
        _tagRepoMock.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        await _service.UpdateTagAsync(tag.Id,
            new UpdateTagRequest("renamed", "#ABCDEF"), "user1", "api:integration");

        Assert.NotEqual(originalModifier, tag.LastModifiedBy);
        Assert.Equal("api:integration", tag.LastModifiedBy);
    }
}
