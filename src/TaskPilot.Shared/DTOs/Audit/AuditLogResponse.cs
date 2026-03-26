namespace TaskPilot.Shared.DTOs.Audit;

public record AuditLogResponse(
    Guid Id,
    Guid ApiKeyId,
    string ApiKeyName,
    DateTime Timestamp,
    string HttpMethod,
    string Endpoint,
    string RequestBodyHash,
    int ResponseStatusCode,
    long DurationMs
);

public record AuditQueryParams(
    Guid? ApiKeyId = null,
    DateTime? From = null,
    DateTime? To = null,
    string? HttpMethod = null,
    int? StatusCodeMin = null,
    int? StatusCodeMax = null,
    int Page = 1,
    int PageSize = 50
);

public record AuditSummaryResponse(
    int TotalRequests,
    int GetsToday,
    int WritesToday,
    int ActiveApiKeys
);
