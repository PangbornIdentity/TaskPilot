using TaskPilot.Entities;
using TaskPilot.Models.Tasks;

namespace TaskPilot.Repositories.Interfaces;

public interface ITaskRepository : IRepository<TaskItem>
{
    Task<(IReadOnlyList<TaskItem> Items, int TotalCount)> GetPagedAsync(
        TaskQueryParams queryParams,
        string userId,
        CancellationToken cancellationToken = default);

    Task<TaskItem?> GetByIdWithTagsAsync(Guid id, string userId, CancellationToken cancellationToken = default);
    Task<TaskItem?> GetByIdWithDetailsAsync(Guid id, string userId, CancellationToken cancellationToken = default);
    Task<int> GetMaxSortOrderAsync(string userId, CancellationToken cancellationToken = default);
}
