using TaskPilot.Shared.DTOs.Audit;
using TaskPilot.Shared.DTOs.Common;

namespace TaskPilot.Client.Services;

public class AuditHttpService
{
    private readonly IHttpClientService _http;
    private const string Base = "api/v1/audit";

    public AuditHttpService(IHttpClientService http)
    {
        _http = http;
    }

    public async Task<PagedApiResponse<AuditLogResponse>?> GetAuditLogsAsync(AuditQueryParams? queryParams = null)
    {
        var url = BuildUrl(queryParams);
        return await _http.GetPagedApiResponseAsync<AuditLogResponse>(url);
    }

    public async Task<AuditSummaryResponse?> GetSummaryAsync()
    {
        var result = await _http.GetApiResponseAsync<AuditSummaryResponse>($"{Base}/summary");
        return result?.Data;
    }

    private static string BuildUrl(AuditQueryParams? p)
    {
        if (p is null) return Base;
        var parts = new List<string>();
        if (p.ApiKeyId is not null) parts.Add($"apiKeyId={p.ApiKeyId}");
        if (p.From is not null) parts.Add($"from={p.From:O}");
        if (p.To is not null) parts.Add($"to={p.To:O}");
        if (!string.IsNullOrEmpty(p.HttpMethod)) parts.Add($"httpMethod={p.HttpMethod}");
        if (p.StatusCodeMin is not null) parts.Add($"statusCodeMin={p.StatusCodeMin}");
        if (p.StatusCodeMax is not null) parts.Add($"statusCodeMax={p.StatusCodeMax}");
        parts.Add($"page={p.Page}");
        parts.Add($"pageSize={p.PageSize}");
        return parts.Count > 0 ? $"{Base}?{string.Join("&", parts)}" : Base;
    }
}
