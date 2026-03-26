using TaskPilot.Server.Entities;
using TaskPilot.Shared.DTOs.Audit;

namespace TaskPilot.Server.Repositories.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(ApiAuditLog log, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ApiAuditLog> Items, int TotalCount)> GetPagedAsync(
        AuditQueryParams queryParams,
        string userId,
        CancellationToken cancellationToken = default);
    Task<AuditSummaryResponse> GetSummaryAsync(string userId, CancellationToken cancellationToken = default);
}
