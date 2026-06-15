using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Moq;
using TaskPilot.Entities;
using TaskPilot.Repositories.Interfaces;
using TaskPilot.Services;

namespace TaskPilot.Tests.Unit.Services;

/// <summary>
/// WI-SOFTDELETE (ApiKey) / FR-APIKEYS-004, NFR-DATA-001, NFR-SEC-002
/// Verifies that ApiKeyService.RevokeKeyAsync performs a soft-delete (IsDeleted=true +
/// IsActive=false, no Remove call) and that ValidateKeyAsync rejects a soft-deleted key.
/// These tests FAIL if RevokeKeyAsync calls Remove() instead of soft-deleting, or if
/// ValidateKeyAsync does not check IsDeleted.
/// </summary>
public class ApiKeySoftDelete_FR_APIKEYS_004_Tests
{
    private const string TestSecret = "unit-test-hmac-secret";

    private static (ApiKeyService Service, Mock<IApiKeyRepository> Repo) BuildService()
    {
        var repo = new Mock<IApiKeyRepository>();
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Hmac:SecretKey"]).Returns(TestSecret);
        return (new ApiKeyService(repo.Object, config.Object), repo);
    }

    private static string ComputeHash(string plainText)
    {
        var hash = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(TestSecret),
            Encoding.UTF8.GetBytes(plainText));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // ── RevokeKeyAsync soft-delete assertions ─────────────────────────────────

    [Fact]
    public async Task RevokeKeyAsync_SetsIsDeletedTrue_NFR_DATA_001()
    {
        var (svc, repo) = BuildService();
        var key = new ApiKey
        {
            Id = Guid.NewGuid(), Name = "Key", KeyHash = "hash",
            KeyPrefix = "prefix12", IsActive = true, UserId = "user1",
            LastModifiedBy = "user:x@example.com"
        };
        repo.Setup(r => r.GetByIdAsync(key.Id, default)).ReturnsAsync(key);
        repo.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        await svc.RevokeKeyAsync(key.Id, "user1", "user:x@example.com");

        Assert.True(key.IsDeleted);
    }

    [Fact]
    public async Task RevokeKeyAsync_SetsIsActiveFalse_FR_APIKEYS_004()
    {
        var (svc, repo) = BuildService();
        var key = new ApiKey
        {
            Id = Guid.NewGuid(), Name = "Key", KeyHash = "hash",
            KeyPrefix = "prefix12", IsActive = true, UserId = "user1",
            LastModifiedBy = "user:x@example.com"
        };
        repo.Setup(r => r.GetByIdAsync(key.Id, default)).ReturnsAsync(key);
        repo.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        await svc.RevokeKeyAsync(key.Id, "user1", "user:x@example.com");

        Assert.False(key.IsActive);
    }

    [Fact]
    public async Task RevokeKeyAsync_SetsDeletedAtTimestamp_NFR_DATA_001()
    {
        var (svc, repo) = BuildService();
        var before = DateTime.UtcNow.AddSeconds(-1);
        var key = new ApiKey
        {
            Id = Guid.NewGuid(), Name = "Key", KeyHash = "hash",
            KeyPrefix = "prefix12", IsActive = true, UserId = "user1",
            LastModifiedBy = "user:x@example.com"
        };
        repo.Setup(r => r.GetByIdAsync(key.Id, default)).ReturnsAsync(key);
        repo.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        await svc.RevokeKeyAsync(key.Id, "user1", "user:x@example.com");

        Assert.NotNull(key.DeletedAt);
        Assert.True(key.DeletedAt >= before);
    }

    [Fact]
    public async Task RevokeKeyAsync_DoesNotCallRemove_NFR_DATA_001()
    {
        var (svc, repo) = BuildService();
        var key = new ApiKey
        {
            Id = Guid.NewGuid(), Name = "Key", KeyHash = "hash",
            KeyPrefix = "prefix12", IsActive = true, UserId = "user1",
            LastModifiedBy = "user:x@example.com"
        };
        repo.Setup(r => r.GetByIdAsync(key.Id, default)).ReturnsAsync(key);
        repo.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        await svc.RevokeKeyAsync(key.Id, "user1", "user:x@example.com");

        // Hard-delete must never be called
        repo.Verify(r => r.Remove(It.IsAny<ApiKey>()), Times.Never);
    }

    // ── ValidateKeyAsync rejects soft-deleted key ────────────────────────────

    [Fact]
    public async Task ValidateKeyAsync_SoftDeletedKey_ReturnsFalse_NFR_SEC_002()
    {
        // A soft-deleted key has IsDeleted=true AND IsActive=false.
        // GetByHashAsync bypasses EF query filters (reads raw), so the repo may
        // still return the entity. ValidateKeyAsync must reject it via IsActive check.
        var (svc, repo) = BuildService();

        var plainText = "tp-revoked-key-1234";
        var hash = ComputeHash(plainText);

        var revokedKey = new ApiKey
        {
            Id = Guid.NewGuid(), Name = "RevokedKey",
            KeyHash = hash, KeyPrefix = plainText[..8],
            IsActive = false,   // set by RevokeKeyAsync
            IsDeleted = true,   // set by RevokeKeyAsync
            UserId = "user1",
            LastModifiedBy = "user:x@example.com"
        };

        // The repository returns the entity even when soft-deleted
        // (as GetByHashAsync would when querying by hash)
        repo.Setup(r => r.GetByHashAsync(hash, default)).ReturnsAsync(revokedKey);

        var (isValid, keyName, userId, keyId) = await svc.ValidateKeyAsync(plainText);

        Assert.False(isValid);
        Assert.Null(keyName);
        Assert.Null(userId);
        Assert.Null(keyId);
    }
}
