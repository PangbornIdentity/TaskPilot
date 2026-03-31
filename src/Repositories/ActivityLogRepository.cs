using Microsoft.EntityFrameworkCore;
using TaskPilot.Data;
using TaskPilot.Models.Audit;
using TaskPilot.Repositories.Interfaces;

namespace TaskPilot.Repositories;

public class ActivityLogRepository(ApplicationDbContext context) : IActivityLogRepository
{
    public async Task<(IReadOnlyList<ActivityLogResponse> Items, int TotalCount)> GetPagedAsync(
        ActivityLogQueryParams queryParams,
        string userId,
        CancellationToken cancellationToken = default)
    {
        // IgnoreQueryFilters so logs for soft-deleted tasks remain visible in the audit trail
        var query = context.TaskActivityLogs
            .Join(context.Tasks.IgnoreQueryFilters(),
                  al => al.TaskId,
                  t => t.Id,
                  (al, t) => new { Log = al, t.Title, t.UserId })
            .Where(x => x.UserId == userId)
            .AsQueryable();

        if (queryParams.TaskId.HasValue)
            query = query.Where(x => x.Log.TaskId == queryParams.TaskId.Value);

        if (queryParams.From.HasValue)
            query = query.Where(x => x.Log.Timestamp >= queryParams.From.Value);

        if (queryParams.To.HasValue)
            query = query.Where(x => x.Log.Timestamp <= queryParams.To.Value);

        if (!string.IsNullOrWhiteSpace(queryParams.FieldChanged))
            query = query.Where(x => x.Log.FieldChanged == queryParams.FieldChanged);

        if (!string.IsNullOrWhiteSpace(queryParams.ChangedBy))
            query = query.Where(x => x.Log.ChangedBy.Contains(queryParams.ChangedBy));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Log.Timestamp)
            .Skip((queryParams.Page - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .Select(x => new ActivityLogResponse(
                x.Log.Id, x.Log.TaskId, x.Title, x.Log.Timestamp,
                x.Log.FieldChanged, x.Log.OldValue, x.Log.NewValue, x.Log.ChangedBy))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<ActivityLogResponse>> GetForTaskAsync(
        Guid taskId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await context.TaskActivityLogs
            .Join(context.Tasks.IgnoreQueryFilters(),
                  al => al.TaskId,
                  t => t.Id,
                  (al, t) => new { Log = al, t.Title, t.UserId })
            .Where(x => x.Log.TaskId == taskId && x.UserId == userId)
            .OrderByDescending(x => x.Log.Timestamp)
            .Select(x => new ActivityLogResponse(
                x.Log.Id, x.Log.TaskId, x.Title, x.Log.Timestamp,
                x.Log.FieldChanged, x.Log.OldValue, x.Log.NewValue, x.Log.ChangedBy))
            .ToListAsync(cancellationToken);
    }
}
