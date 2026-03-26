using TaskPilot.Server.Entities;
using TaskPilot.Server.Repositories.Interfaces;
using TaskPilot.Server.Services.Interfaces;
using TaskPilot.Shared.DTOs.Tags;

namespace TaskPilot.Server.Services;

public class TagService(ITagRepository tagRepository) : ITagService
{
    public async Task<IReadOnlyList<TagResponse>> GetAllTagsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var tags = await tagRepository.GetAllForUserAsync(userId, cancellationToken);
        return tags.Select(MapToResponse).ToList();
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
        return MapToResponse(tag);
    }

    public async Task<bool> DeleteTagAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        var tag = await tagRepository.GetByIdAsync(id, cancellationToken);
        if (tag is null || tag.UserId != userId) return false;

        tagRepository.Remove(tag);
        await tagRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static TagResponse MapToResponse(Tag tag) => new(tag.Id, tag.Name, tag.Color, tag.CreatedDate);
}
