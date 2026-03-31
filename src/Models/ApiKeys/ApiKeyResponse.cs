namespace TaskPilot.Models.ApiKeys;

public record ApiKeyResponse(
    Guid Id,
    string Name,
    string KeyPrefix,
    DateTime CreatedDate,
    DateTime? LastUsedDate,
    bool IsActive
);

public record CreateApiKeyResponse(
    Guid Id,
    string Name,
    string KeyPrefix,
    string PlainTextKey,
    DateTime CreatedDate
);
