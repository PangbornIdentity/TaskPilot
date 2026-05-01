using TaskPilot.Entities;
using TaskPilot.Repositories.Interfaces;
using TaskPilot.Services.Interfaces;
using TaskPilot.Models.Tags;

namespace TaskPilot.Services;

public class TagService(ITagRepository tagRepository) : ITagService
{
    public async Task<IReadOnlyList<TagResponse>> GetAllTagsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var rows = await tagRepository.GetAllForUserWithTaskCountAsync(userId, cancellationToken);
        return rows.Select(r => MapToResponse(r.Tag, r.TaskCount)).ToList();
    }

    public async Task<TagResponse> CreateTagAsync(CreateTagRequest request, string userId, string modifiedBy, CancellationToken cancellationToken = default)
    {
        var existing = await tagRepository.GetByNameAsync(request.Name, userId, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException($"A tag named '{request.Name}' already exists.");

        var tag = new Tag
        {
            Name = request.Name,
            Color = request.Color,
            UserId = userId,
            LastModifiedBy = modifiedBy
        };

        await tagRepository.AddAsync(tag, cancellationToken);
        await tagRepository.SaveChangesAsync(cancellationToken);
        return MapToResponse(tag, taskCount: 0);
    }

    public async Task<TagResponse?> UpdateTagAsync(Guid id, UpdateTagRequest request, string userId, string modifiedBy, CancellationToken cancellationToken = default)
    {
        var tag = await tagRepository.GetByIdAsync(id, cancellationToken);
        if (tag is null || tag.UserId != userId) return null;

        if (!string.Equals(tag.Name, request.Name, StringComparison.Ordinal))
        {
            var conflict = await tagRepository.GetByNameAsync(request.Name, userId, cancellationToken);
            if (conflict is not null && conflict.Id != id)
                throw new InvalidOperationException($"A tag named '{request.Name}' already exists.");
        }

        tag.Name = request.Name;
        tag.Color = request.Color;
        tag.LastModifiedBy = modifiedBy;

        await tagRepository.SaveChangesAsync(cancellationToken);

        var refreshed = await tagRepository.GetAllForUserWithTaskCountAsync(userId, cancellationToken);
        var match = refreshed.FirstOrDefault(r => r.Tag.Id == id);
        return MapToResponse(tag, match.TaskCount);
    }

    public async Task<bool> DeleteTagAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        var tag = await tagRepository.GetByIdAsync(id, cancellationToken);
        if (tag is null || tag.UserId != userId) return false;

        tagRepository.Remove(tag);
        await tagRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static TagResponse MapToResponse(Tag tag, int taskCount = 0)
        => new(tag.Id, tag.Name, tag.Color, tag.CreatedDate, taskCount);
}
