using TaskPilot.Models.Audit;
using TaskPilot.Models.Common;

namespace TaskPilot.Services.Interfaces;

public interface IAuditService
{
    Task<PagedApiResponse<AuditLogResponse>> GetAuditLogsAsync(AuditQueryParams queryParams, string userId, CancellationToken cancellationToken = default);
    Task<AuditSummaryResponse> GetSummaryAsync(string userId, CancellationToken cancellationToken = default);
}
