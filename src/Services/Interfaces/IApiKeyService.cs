using TaskPilot.Models.ApiKeys;

namespace TaskPilot.Services.Interfaces;

public interface IApiKeyService
{
    Task<IReadOnlyList<ApiKeyResponse>> GetAllKeysAsync(string userId, CancellationToken cancellationToken = default);
    Task<CreateApiKeyResponse> GenerateKeyAsync(CreateApiKeyRequest request, string userId, string modifiedBy, CancellationToken cancellationToken = default);
    Task<bool> RenameKeyAsync(Guid id, RenameApiKeyRequest request, string userId, string modifiedBy, CancellationToken cancellationToken = default);
    Task<bool> SetActiveStateAsync(Guid id, bool isActive, string userId, string modifiedBy, CancellationToken cancellationToken = default);
    Task<bool> RevokeKeyAsync(Guid id, string userId, CancellationToken cancellationToken = default);
    Task<(bool IsValid, string? KeyName, string? UserId)> ValidateKeyAsync(string plainTextKey, CancellationToken cancellationToken = default);
}
