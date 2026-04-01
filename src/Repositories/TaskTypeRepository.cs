using Microsoft.EntityFrameworkCore;
using TaskPilot.Data;
using TaskPilot.Entities;
using TaskPilot.Repositories.Interfaces;

namespace TaskPilot.Repositories;

public class TaskTypeRepository(ApplicationDbContext context) : ITaskTypeRepository
{
    public async Task<IReadOnlyList<TaskType>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        => await context.TaskTypes
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder)
            .ToListAsync(cancellationToken);
}
