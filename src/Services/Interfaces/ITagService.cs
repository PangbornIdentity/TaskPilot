using TaskPilot.Models.Tags;

namespace TaskPilot.Services.Interfaces;

public interface ITagService
{
    Task<IReadOnlyList<TagResponse>> GetAllTagsAsync(string userId, CancellationToken cancellationToken = default);
    Task<TagResponse> CreateTagAsync(CreateTagRequest request, string userId, string modifiedBy, CancellationToken cancellationToken = default);
    Task<bool> DeleteTagAsync(Guid id, string userId, CancellationToken cancellationToken = default);
}
