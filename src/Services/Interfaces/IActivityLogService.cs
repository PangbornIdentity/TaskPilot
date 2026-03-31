using TaskPilot.Models.Audit;
using TaskPilot.Models.Common;

namespace TaskPilot.Services.Interfaces;

public interface IActivityLogService
{
    Task<PagedApiResponse<ActivityLogResponse>> GetPagedAsync(
        ActivityLogQueryParams queryParams,
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ActivityLogResponse>> GetForTaskAsync(
        Guid taskId,
        string userId,
        CancellationToken cancellationToken = default);
}
