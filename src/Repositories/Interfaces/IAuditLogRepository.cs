using TaskPilot.Entities;
using TaskPilot.Models.Audit;

namespace TaskPilot.Repositories.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(ApiAuditLog log, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ApiAuditLog> Items, int TotalCount)> GetPagedAsync(
        AuditQueryParams queryParams,
        string userId,
        CancellationToken cancellationToken = default);
    Task<AuditSummaryResponse> GetSummaryAsync(string userId, CancellationToken cancellationToken = default);
}
