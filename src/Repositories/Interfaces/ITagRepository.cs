using TaskPilot.Entities;

namespace TaskPilot.Repositories.Interfaces;

public interface ITagRepository : IRepository<Tag>
{
    Task<IReadOnlyList<Tag>> GetAllForUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<Tag?> GetByNameAsync(string name, string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tag>> GetByIdsAsync(IEnumerable<Guid> ids, string userId, CancellationToken cancellationToken = default);
}
