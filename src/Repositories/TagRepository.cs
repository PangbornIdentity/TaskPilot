using Microsoft.EntityFrameworkCore;
using TaskPilot.Data;
using TaskPilot.Entities;
using TaskPilot.Repositories.Interfaces;

namespace TaskPilot.Repositories;

public class TagRepository(ApplicationDbContext context) : GenericRepository<Tag>(context), ITagRepository
{
    public async Task<IReadOnlyList<Tag>> GetAllForUserAsync(string userId, CancellationToken cancellationToken = default)
        => await Context.Tags
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<(Tag Tag, int TaskCount)>> GetAllForUserWithTaskCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        var rows = await Context.Tags
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Name)
            .Select(t => new { Tag = t, TaskCount = t.TaskTags.Count(tt => !tt.Task.IsDeleted) })
            .ToListAsync(cancellationToken);

        return rows.Select(r => (r.Tag, r.TaskCount)).ToList();
    }

    public async Task<Tag?> GetByNameAsync(string name, string userId, CancellationToken cancellationToken = default)
        => await Context.Tags
            .FirstOrDefaultAsync(t => t.Name == name && t.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<Tag>> GetByIdsAsync(IEnumerable<Guid> ids, string userId, CancellationToken cancellationToken = default)
        => await Context.Tags
            .Where(t => ids.Contains(t.Id) && t.UserId == userId)
            .ToListAsync(cancellationToken);
}
