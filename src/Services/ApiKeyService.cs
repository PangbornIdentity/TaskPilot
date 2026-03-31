using System.Security.Cryptography;
using System.Text;
using TaskPilot.Entities;
using TaskPilot.Repositories.Interfaces;
using TaskPilot.Services.Interfaces;
using TaskPilot.Models.ApiKeys;

namespace TaskPilot.Services;

public class ApiKeyService(IApiKeyRepository apiKeyRepository, IConfiguration configuration) : IApiKeyService
{
    public async Task<IReadOnlyList<ApiKeyResponse>> GetAllKeysAsync(string userId, CancellationToken cancellationToken = default)
    {
        var keys = await apiKeyRepository.GetAllForUserAsync(userId, cancellationToken);
        return keys.Select(MapToResponse).ToList();
    }

    public async Task<CreateApiKeyResponse> GenerateKeyAsync(CreateApiKeyRequest request, string userId, string modifiedBy, CancellationToken cancellationToken = default)
    {
        var plainTextKey = GeneratePlainTextKey();
        var keyHash = ComputeHmacHash(plainTextKey);
        var keyPrefix = plainTextKey[..8];

        var apiKey = new ApiKey
        {
            Name = request.Name,
            KeyHash = keyHash,
            KeyPrefix = keyPrefix,
            IsActive = true,
            UserId = userId,
            LastModifiedBy = modifiedBy
        };

        await apiKeyRepository.AddAsync(apiKey, cancellationToken);
        await apiKeyRepository.SaveChangesAsync(cancellationToken);

        return new CreateApiKeyResponse(apiKey.Id, apiKey.Name, apiKey.KeyPrefix, plainTextKey, apiKey.CreatedDate);
    }

    public async Task<bool> RenameKeyAsync(Guid id, RenameApiKeyRequest request, string userId, string modifiedBy, CancellationToken cancellationToken = default)
    {
        var key = await apiKeyRepository.GetByIdAsync(id, cancellationToken);
        if (key is null || key.UserId != userId) return false;

        key.Name = request.Name;
        key.LastModifiedBy = modifiedBy;
        await apiKeyRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetActiveStateAsync(Guid id, bool isActive, string userId, string modifiedBy, CancellationToken cancellationToken = default)
    {
        var key = await apiKeyRepository.GetByIdAsync(id, cancellationToken);
        if (key is null || key.UserId != userId) return false;

        key.IsActive = isActive;
        key.LastModifiedBy = modifiedBy;
        await apiKeyRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RevokeKeyAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        var key = await apiKeyRepository.GetByIdAsync(id, cancellationToken);
        if (key is null || key.UserId != userId) return false;

        apiKeyRepository.Remove(key);
        await apiKeyRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(bool IsValid, string? KeyName, string? UserId)> ValidateKeyAsync(string plainTextKey, CancellationToken cancellationToken = default)
    {
        var keyHash = ComputeHmacHash(plainTextKey);
        var apiKey = await apiKeyRepository.GetByHashAsync(keyHash, cancellationToken);

        if (apiKey is null || !apiKey.IsActive)
            return (false, null, null);

        _ = apiKeyRepository.UpdateLastUsedAsync(apiKey.Id, cancellationToken);
        return (true, apiKey.Name, apiKey.UserId);
    }

    private string ComputeHmacHash(string plainTextKey)
    {
        var secret = configuration["Hmac:SecretKey"]
            ?? throw new InvalidOperationException("HMAC secret key is not configured.");
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var keyBytes = Encoding.UTF8.GetBytes(plainTextKey);
        var hash = HMACSHA256.HashData(secretBytes, keyBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string GeneratePlainTextKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private static ApiKeyResponse MapToResponse(ApiKey key) =>
        new(key.Id, key.Name, key.KeyPrefix, key.CreatedDate, key.LastUsedDate, key.IsActive);
}
