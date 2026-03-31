using TaskPilot.Models.Audit;
using TaskPilot.Models.Common;
using TaskPilot.Repositories.Interfaces;
using TaskPilot.Services.Interfaces;

namespace TaskPilot.Services;

public class ActivityLogService(IActivityLogRepository activityLogRepository) : IActivityLogService
{
    public async Task<PagedApiResponse<ActivityLogResponse>> GetPagedAsync(
        ActivityLogQueryParams queryParams,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await activityLogRepository.GetPagedAsync(queryParams, userId, cancellationToken);
        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)queryParams.PageSize);
        var meta = new PagedResponseMeta(DateTime.UtcNow, Guid.NewGuid().ToString(),
            queryParams.Page, queryParams.PageSize, totalCount, totalPages);
        return new PagedApiResponse<ActivityLogResponse>(items, meta);
    }

    public Task<IReadOnlyList<ActivityLogResponse>> GetForTaskAsync(
        Guid taskId,
        string userId,
        CancellationToken cancellationToken = default)
        => activityLogRepository.GetForTaskAsync(taskId, userId, cancellationToken);
}
