using TaskPilot.Entities;

namespace TaskPilot.Repositories.Interfaces;

public interface ITagRepository : IRepository<Tag>
{
    Task<IReadOnlyList<Tag>> GetAllForUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(Tag Tag, int TaskCount)>> GetAllForUserWithTaskCountAsync(string userId, CancellationToken cancellationToken = default);
    Task<Tag?> GetByNameAsync(string name, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the tag matching (name, userId) regardless of soft-delete state.
    /// Used by CreateTagAsync to detect tombstones that should be resurrected
    /// rather than inserted (which would violate the unfiltered unique index).
    /// </summary>
    Task<Tag?> GetByNameIncludingDeletedAsync(string name, string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Tag>> GetByIdsAsync(IEnumerable<Guid> ids, string userId, CancellationToken cancellationToken = default);
}
