using TaskPilot.Models.Audit;

namespace TaskPilot.Repositories.Interfaces;

public interface IActivityLogRepository
{
    Task<(IReadOnlyList<ActivityLogResponse> Items, int TotalCount)> GetPagedAsync(
        ActivityLogQueryParams queryParams,
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ActivityLogResponse>> GetForTaskAsync(
        Guid taskId,
        string userId,
        CancellationToken cancellationToken = default);
}
