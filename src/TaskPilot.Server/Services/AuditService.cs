using TaskPilot.Server.Repositories.Interfaces;
using TaskPilot.Server.Services.Interfaces;
using TaskPilot.Shared.DTOs.Audit;
using TaskPilot.Shared.DTOs.Common;

namespace TaskPilot.Server.Services;

public class AuditService(IAuditLogRepository auditLogRepository) : IAuditService
{
    public async Task<PagedApiResponse<AuditLogResponse>> GetAuditLogsAsync(AuditQueryParams queryParams, string userId, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await auditLogRepository.GetPagedAsync(queryParams, userId, cancellationToken);
        var responses = items.Select(a => new AuditLogResponse(
            a.Id, a.ApiKeyId, a.ApiKeyName, a.Timestamp, a.HttpMethod, a.Endpoint, a.RequestBodyHash, a.ResponseStatusCode, a.DurationMs
        )).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)queryParams.PageSize);
        var meta = new PagedResponseMeta(DateTime.UtcNow, Guid.NewGuid().ToString(), queryParams.Page, queryParams.PageSize, totalCount, totalPages);
        return new PagedApiResponse<AuditLogResponse>(responses, meta);
    }

    public Task<AuditSummaryResponse> GetSummaryAsync(string userId, CancellationToken cancellationToken = default)
        => auditLogRepository.GetSummaryAsync(userId, cancellationToken);
}
