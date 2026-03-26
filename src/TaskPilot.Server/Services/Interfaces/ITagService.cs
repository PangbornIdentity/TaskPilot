using TaskPilot.Shared.DTOs.Tags;

namespace TaskPilot.Server.Services.Interfaces;

public interface ITagService
{
    Task<IReadOnlyList<TagResponse>> GetAllTagsAsync(string userId, CancellationToken cancellationToken = default);
    Task<TagResponse> CreateTagAsync(CreateTagRequest request, string userId, string modifiedBy, CancellationToken cancellationToken = default);
    Task<bool> DeleteTagAsync(Guid id, string userId, CancellationToken cancellationToken = default);
}
