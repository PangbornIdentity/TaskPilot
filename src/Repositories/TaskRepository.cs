using Microsoft.EntityFrameworkCore;
using TaskPilot.Data;
using TaskPilot.Entities;
using TaskPilot.Repositories.Interfaces;
using TaskPilot.Models.Tasks;
using TaskPilot.Models.Enums;

namespace TaskPilot.Repositories;

public class TaskRepository(ApplicationDbContext context) : GenericRepository<TaskItem>(context), ITaskRepository
{
    public async Task<(IReadOnlyList<TaskItem> Items, int TotalCount)> GetPagedAsync(
        TaskQueryParams queryParams,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Tasks
            .Include(t => t.TaskType)
            .Include(t => t.TaskTags)
            .ThenInclude(tt => tt.Tag)
            .Where(t => t.UserId == userId)
            .AsQueryable();

        if (queryParams.Status.HasValue)
            query = query.Where(t => t.Status == queryParams.Status.Value);

        if (queryParams.TaskTypeId.HasValue)
            query = query.Where(t => t.TaskTypeId == queryParams.TaskTypeId.Value);

        if (queryParams.Area.HasValue)
            query = query.Where(t => t.Area == queryParams.Area.Value);

        if (queryParams.Priority.HasValue)
            query = query.Where(t => t.Priority == queryParams.Priority.Value);

        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var search = queryParams.Search.ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(search) ||
                                     (t.Description != null && t.Description.ToLower().Contains(search)));
        }

        if (queryParams.IsRecurring.HasValue)
            query = query.Where(t => t.IsRecurring == queryParams.IsRecurring.Value);

        if (queryParams.TagIds?.Count > 0)
        {
            foreach (var tagId in queryParams.TagIds)
            {
                var capturedId = tagId;
                query = query.Where(t => t.TaskTags.Any(tt => tt.TagId == capturedId));
            }
        }

        query = queryParams.SortBy.ToLower() switch
        {
            "targetdate" => queryParams.SortDir == "desc"
                ? query.OrderByDescending(t => t.TargetDate)
                : query.OrderBy(t => t.TargetDate),
            "createddate" => queryParams.SortDir == "desc"
                ? query.OrderByDescending(t => t.CreatedDate)
                : query.OrderBy(t => t.CreatedDate),
            "lastmodifieddate" => queryParams.SortDir == "desc"
                ? query.OrderByDescending(t => t.LastModifiedDate)
                : query.OrderBy(t => t.LastModifiedDate),
            _ => queryParams.SortDir == "desc"
                ? query.OrderByDescending(t => t.Priority).ThenBy(t => t.SortOrder)
                : query.OrderBy(t => t.Priority).ThenBy(t => t.SortOrder)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((queryParams.Page - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<TaskItem?> GetByIdWithTagsAsync(Guid id, string userId, CancellationToken cancellationToken = default)
        => await Context.Tasks
            .Include(t => t.TaskType)
            .Include(t => t.TaskTags)
            .ThenInclude(tt => tt.Tag)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);

    public async Task<TaskItem?> GetByIdWithDetailsAsync(Guid id, string userId, CancellationToken cancellationToken = default)
        => await Context.Tasks
            .Include(t => t.TaskType)
            .Include(t => t.TaskTags)
            .ThenInclude(tt => tt.Tag)
            .Include(t => t.ActivityLogs.OrderByDescending(a => a.Timestamp))
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);

    public async Task<int> GetMaxSortOrderAsync(string userId, CancellationToken cancellationToken = default)
    {
        var max = await Context.Tasks
            .Where(t => t.UserId == userId)
            .MaxAsync(t => (int?)t.SortOrder, cancellationToken);
        return max ?? 0;
    }
}
