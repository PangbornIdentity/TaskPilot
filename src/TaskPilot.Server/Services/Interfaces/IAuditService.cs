using TaskPilot.Shared.DTOs.Audit;
using TaskPilot.Shared.DTOs.Common;

namespace TaskPilot.Server.Services.Interfaces;

public interface IAuditService
{
    Task<PagedApiResponse<AuditLogResponse>> GetAuditLogsAsync(AuditQueryParams queryParams, string userId, CancellationToken cancellationToken = default);
    Task<AuditSummaryResponse> GetSummaryAsync(string userId, CancellationToken cancellationToken = default);
}
