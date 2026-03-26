using Microsoft.EntityFrameworkCore;
using TaskPilot.Server.Data;
using TaskPilot.Server.Entities;
using TaskPilot.Server.Repositories.Interfaces;
using TaskPilot.Shared.DTOs.Audit;

namespace TaskPilot.Server.Repositories;

public class AuditLogRepository(ApplicationDbContext context) : IAuditLogRepository
{
    public async Task AddAsync(ApiAuditLog log, CancellationToken cancellationToken = default)
    {
        await context.ApiAuditLogs.AddAsync(log, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<ApiAuditLog> Items, int TotalCount)> GetPagedAsync(
        AuditQueryParams queryParams,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var query = context.ApiAuditLogs
            .Where(a => a.UserId == userId)
            .AsQueryable();

        if (queryParams.ApiKeyId.HasValue)
            query = query.Where(a => a.ApiKeyId == queryParams.ApiKeyId.Value);

        if (queryParams.From.HasValue)
            query = query.Where(a => a.Timestamp >= queryParams.From.Value);

        if (queryParams.To.HasValue)
            query = query.Where(a => a.Timestamp <= queryParams.To.Value);

        if (!string.IsNullOrWhiteSpace(queryParams.HttpMethod))
            query = query.Where(a => a.HttpMethod == queryParams.HttpMethod);

        if (queryParams.StatusCodeMin.HasValue)
            query = query.Where(a => a.ResponseStatusCode >= queryParams.StatusCodeMin.Value);

        if (queryParams.StatusCodeMax.HasValue)
            query = query.Where(a => a.ResponseStatusCode <= queryParams.StatusCodeMax.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((queryParams.Page - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<AuditSummaryResponse> GetSummaryAsync(string userId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        var totalRequests = await context.ApiAuditLogs
            .CountAsync(a => a.UserId == userId, cancellationToken);

        var getsToday = await context.ApiAuditLogs
            .CountAsync(a => a.UserId == userId && a.HttpMethod == "GET" && a.Timestamp >= today, cancellationToken);

        var writesToday = await context.ApiAuditLogs
            .CountAsync(a => a.UserId == userId && a.HttpMethod != "GET" && a.Timestamp >= today, cancellationToken);

        var activeApiKeys = await context.ApiKeys
            .CountAsync(k => k.UserId == userId && k.IsActive, cancellationToken);

        return new AuditSummaryResponse(totalRequests, getsToday, writesToday, activeApiKeys);
    }
}
