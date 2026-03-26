using Microsoft.EntityFrameworkCore;
using TaskPilot.Server.Data;
using TaskPilot.Server.Entities;
using TaskPilot.Server.Repositories.Interfaces;

namespace TaskPilot.Server.Repositories;

public class TagRepository(ApplicationDbContext context) : GenericRepository<Tag>(context), ITagRepository
{
    public async Task<IReadOnlyList<Tag>> GetAllForUserAsync(string userId, CancellationToken cancellationToken = default)
        => await Context.Tags
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

    public async Task<Tag?> GetByNameAsync(string name, string userId, CancellationToken cancellationToken = default)
        => await Context.Tags
            .FirstOrDefaultAsync(t => t.Name == name && t.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<Tag>> GetByIdsAsync(IEnumerable<Guid> ids, string userId, CancellationToken cancellationToken = default)
        => await Context.Tags
            .Where(t => ids.Contains(t.Id) && t.UserId == userId)
            .ToListAsync(cancellationToken);
}
