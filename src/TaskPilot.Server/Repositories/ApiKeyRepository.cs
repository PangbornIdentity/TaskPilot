using Microsoft.EntityFrameworkCore;
using TaskPilot.Server.Data;
using TaskPilot.Server.Entities;
using TaskPilot.Server.Repositories.Interfaces;

namespace TaskPilot.Server.Repositories;

public class ApiKeyRepository(ApplicationDbContext context) : GenericRepository<ApiKey>(context), IApiKeyRepository
{
    public async Task<ApiKey?> GetByHashAsync(string keyHash, CancellationToken cancellationToken = default)
        => await Context.ApiKeys
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash, cancellationToken);

    public async Task<IReadOnlyList<ApiKey>> GetAllForUserAsync(string userId, CancellationToken cancellationToken = default)
        => await Context.ApiKeys
            .Where(k => k.UserId == userId)
            .OrderBy(k => k.Name)
            .ToListAsync(cancellationToken);

    public async Task UpdateLastUsedAsync(Guid id, CancellationToken cancellationToken = default)
        => await Context.ApiKeys
            .Where(k => k.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(k => k.LastUsedDate, DateTime.UtcNow), cancellationToken);
}
