using TaskPilot.Entities;

namespace TaskPilot.Repositories.Interfaces;

public interface IApiKeyRepository : IRepository<ApiKey>
{
    Task<ApiKey?> GetByHashAsync(string keyHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApiKey>> GetAllForUserAsync(string userId, CancellationToken cancellationToken = default);
    Task UpdateLastUsedAsync(Guid id, CancellationToken cancellationToken = default);
}
