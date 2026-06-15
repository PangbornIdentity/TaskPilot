using Microsoft.EntityFrameworkCore;
using Moq;
using TaskPilot.Entities;
using TaskPilot.Repositories.Interfaces;
using TaskPilot.Services;
using TaskPilot.Tests.Unit.Helpers;

namespace TaskPilot.Tests.Unit.Services;

/// <summary>
/// WI-SOFTDELETE (Tag) / FR-TAGS-004, NFR-DATA-001
/// Verifies that TagService.DeleteTagAsync performs a soft-delete and that
/// soft-deleted tags are excluded from subsequent reads but TaskTag join rows survive.
/// These tests FAIL if the service calls repository.Remove() or if the
/// query filter is not applied.
/// </summary>
public class TagSoftDelete_FR_TAGS_004_Tests
{
    // ── Soft-delete via mocked repository ────────────────────────────────────

    [Fact]
    public async Task DeleteTagAsync_SetsIsDeletedTrue_FR_TAGS_004()
    {
        var repo = new Mock<ITagRepository>();
        var svc = new TagService(repo.Object);

        var tag = new Tag
        {
            Id = Guid.NewGuid(), Name = "ToDelete", Color = "#000000",
            UserId = "user1", LastModifiedBy = "user:test@example.com"
        };
        repo.Setup(r => r.GetByIdAsync(tag.Id, default)).ReturnsAsync(tag);
        repo.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        await svc.DeleteTagAsync(tag.Id, "user1", "user:test@example.com");

        Assert.True(tag.IsDeleted);
    }

    [Fact]
    public async Task DeleteTagAsync_SetsDeletedAtTimestamp_FR_TAGS_004()
    {
        var repo = new Mock<ITagRepository>();
        var svc = new TagService(repo.Object);

        var before = DateTime.UtcNow.AddSeconds(-1);
        var tag = new Tag
        {
            Id = Guid.NewGuid(), Name = "Timed", Color = "#000000",
            UserId = "user1", LastModifiedBy = "user:test@example.com"
        };
        repo.Setup(r => r.GetByIdAsync(tag.Id, default)).ReturnsAsync(tag);
        repo.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        await svc.DeleteTagAsync(tag.Id, "user1", "user:test@example.com");

        Assert.NotNull(tag.DeletedAt);
        Assert.True(tag.DeletedAt >= before);
    }

    [Fact]
    public async Task DeleteTagAsync_DoesNotCallRemove_NFR_DATA_001()
    {
        var repo = new Mock<ITagRepository>();
        var svc = new TagService(repo.Object);

        var tag = new Tag
        {
            Id = Guid.NewGuid(), Name = "NoRemove", Color = "#000000",
            UserId = "user1", LastModifiedBy = "user:test@example.com"
        };
        repo.Setup(r => r.GetByIdAsync(tag.Id, default)).ReturnsAsync(tag);
        repo.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        await svc.DeleteTagAsync(tag.Id, "user1", "user:test@example.com");

        // Remove must never be called — this is a soft-delete only
        repo.Verify(r => r.Remove(It.IsAny<Tag>()), Times.Never);
    }

    // ── Soft-deleted tag excluded from GetAllTagsForUser ─────────────────────

    [Fact]
    public async Task GetAllTagsAsync_ExcludesSoftDeletedTag_FR_TAGS_004()
    {
        // The real query filter (HasQueryFilter(t => !t.IsDeleted)) is applied by EF.
        // Use the in-memory context to exercise it end-to-end with the actual repository.
        using var context = TestDbContextFactory.Create();
        var tagRepo = new TaskPilot.Repositories.TagRepository(context);
        var svc = new TagService(tagRepo);

        // Seed one live tag + one soft-deleted tag
        var liveTag = new Tag
        {
            Id = Guid.NewGuid(), Name = "Live", Color = "#111111",
            UserId = "user1", LastModifiedBy = "user:test@example.com",
            IsDeleted = false
        };
        var deadTag = new Tag
        {
            Id = Guid.NewGuid(), Name = "Dead", Color = "#222222",
            UserId = "user1", LastModifiedBy = "user:test@example.com",
            IsDeleted = true, DeletedAt = DateTime.UtcNow
        };
        context.Tags.AddRange(liveTag, deadTag);
        await context.SaveChangesAsync();

        var result = await svc.GetAllTagsAsync("user1");

        Assert.Single(result);
        Assert.Equal("Live", result[0].Name);
        Assert.DoesNotContain(result, t => t.Name == "Dead");
    }

    // ── TaskTag join rows survive soft-delete (use IgnoreQueryFilters) ────────

    [Fact]
    public async Task DeleteTagAsync_PreservesTaskTagJoinRows_NFR_DATA_001()
    {
        using var context = TestDbContextFactory.Create();
        var tagRepo = new TaskPilot.Repositories.TagRepository(context);
        var svc = new TagService(tagRepo);

        // Seed a TaskType (required FK)
        var taskType = new TaskType { Id = 99, Name = "Task", SortOrder = 1, IsActive = true };
        context.TaskTypes.Add(taskType);

        var tag = new Tag
        {
            Id = Guid.NewGuid(), Name = "WithTask", Color = "#333333",
            UserId = "user1", LastModifiedBy = "user:test@example.com"
        };
        context.Tags.Add(tag);

        var task = new TaskItem
        {
            Id = Guid.NewGuid(), Title = "Owner Task", UserId = "user1",
            LastModifiedBy = "user:test@example.com",
            Area = TaskPilot.Models.Enums.Area.Personal,
            Priority = TaskPilot.Models.Enums.TaskPriority.Medium,
            Status = TaskPilot.Models.Enums.TaskStatus.NotStarted,
            TargetDateType = TaskPilot.Models.Enums.TargetDateType.ThisWeek,
            TaskTypeId = 99
        };
        context.Tasks.Add(task);

        var join = new TaskTag { TaskId = task.Id, TagId = tag.Id };
        context.TaskTags.Add(join);
        await context.SaveChangesAsync();

        // Soft-delete the tag
        var deleted = await svc.DeleteTagAsync(tag.Id, "user1", "user:test@example.com");
        Assert.True(deleted);

        // The TaskTag join row must still exist in the DB (query with IgnoreQueryFilters)
        var joinRows = await context.TaskTags
            .IgnoreQueryFilters()
            .Where(tt => tt.TagId == tag.Id)
            .ToListAsync();

        Assert.Single(joinRows);
        Assert.Equal(task.Id, joinRows[0].TaskId);
    }
}
