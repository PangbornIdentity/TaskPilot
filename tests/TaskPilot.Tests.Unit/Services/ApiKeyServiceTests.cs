using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Moq;
using TaskPilot.Server.Entities;
using TaskPilot.Server.Repositories.Interfaces;
using TaskPilot.Server.Services;
using TaskPilot.Shared.DTOs.ApiKeys;

namespace TaskPilot.Tests.Unit.Services;

public class ApiKeyServiceTests
{
    private readonly Mock<IApiKeyRepository> _apiKeyRepoMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly ApiKeyService _service;
    private const string TestSecret = "test-hmac-secret-key";

    public ApiKeyServiceTests()
    {
        _apiKeyRepoMock = new Mock<IApiKeyRepository>();
        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["Hmac:SecretKey"]).Returns(TestSecret);
        _service = new ApiKeyService(_apiKeyRepoMock.Object, _configMock.Object);
    }

    private string ComputeHash(string plainText)
    {
        var secretBytes = Encoding.UTF8.GetBytes(TestSecret);
        var keyBytes = Encoding.UTF8.GetBytes(plainText);
        var hash = HMACSHA256.HashData(secretBytes, keyBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    [Fact]
    public async Task GenerateKeyAsync_ReturnsKeyWithPlainTextOnce()
    {
        var request = new CreateApiKeyRequest("My API Key");

        _apiKeyRepoMock.Setup(r => r.AddAsync(It.IsAny<ApiKey>(), default)).Returns(Task.CompletedTask);
        _apiKeyRepoMock.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _service.GenerateKeyAsync(request, "user1", "user:test@example.com");

        Assert.NotNull(result);
        Assert.NotEmpty(result.PlainTextKey);
        Assert.Equal("My API Key", result.Name);
    }

    [Fact]
    public async Task GenerateKeyAsync_StoresHashNotPlainText()
    {
        var request = new CreateApiKeyRequest("Stored Key");
        ApiKey? capturedKey = null;

        _apiKeyRepoMock.Setup(r => r.AddAsync(It.IsAny<ApiKey>(), default))
            .Callback<ApiKey, CancellationToken>((k, _) => capturedKey = k)
            .Returns(Task.CompletedTask);
        _apiKeyRepoMock.Setup(r => r.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _service.GenerateKeyAsync(request, "user1", "user:test@example.com");

        Assert.NotNull(capturedKey);
        // Hash should not equal plain text key
        Assert.NotEqual(result.PlainTextKey, capturedKey.KeyHash);
        // Hash should be hex string (64 chars for SHA256)
        Assert.Equal(64, capturedKey.KeyHash.Length);
        // Verify the stored hash matches computed hash from plain text
        Assert.Equal(ComputeHash(result.PlainTextKey), capturedKey.KeyHash);
    }

    [Fact]
    public async Task ValidateKeyAsync_ValidKey_ReturnsTrue()
    {
        var plainText = "tp-testkey12345";
        var hash = ComputeHash(plainText);
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            Name = "Test Key",
            KeyHash = hash,
            KeyPrefix = plainText[..8],
            IsActive = true,
            UserId = "user1",
            LastModifiedBy = "user:test@example.com"
        };

        _apiKeyRepoMock.Setup(r => r.GetByHashAsync(hash, default)).ReturnsAsync(apiKey);
        _apiKeyRepoMock.Setup(r => r.UpdateLastUsedAsync(apiKey.Id, default)).Returns(Task.CompletedTask);

        var (isValid, keyName, userId) = await _service.ValidateKeyAsync(plainText);

        Assert.True(isValid);
        Assert.Equal("Test Key", keyName);
        Assert.Equal("user1", userId);
    }

    [Fact]
    public async Task ValidateKeyAsync_InactiveKey_ReturnsFalse()
    {
        var plainText = "tp-inactivekey12";
        var hash = ComputeHash(plainText);
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            Name = "Inactive Key",
            KeyHash = hash,
            KeyPrefix = plainText[..8],
            IsActive = false,
            UserId = "user1",
            LastModifiedBy = "user:test@example.com"
        };

        _apiKeyRepoMock.Setup(r => r.GetByHashAsync(hash, default)).ReturnsAsync(apiKey);

        var (isValid, keyName, userId) = await _service.ValidateKeyAsync(plainText);

        Assert.False(isValid);
        Assert.Null(keyName);
        Assert.Null(userId);
    }

    [Fact]
    public async Task ValidateKeyAsync_UnknownKey_ReturnsFalse()
    {
        var plainText = "tp-unknownkey999";
        var hash = ComputeHash(plainText);

        _apiKeyRepoMock.Setup(r => r.GetByHashAsync(hash, default)).ReturnsAsync((ApiKey?)null);

        var (isValid, keyName, userId) = await _service.ValidateKeyAsync(plainText);

        Assert.False(isValid);
        Assert.Null(keyName);
        Assert.Null(userId);
    }

    [Fact]
    public async Task SetActiveStateAsync_WrongUser_ReturnsFalse()
    {
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            Name = "Key",
            KeyHash = "hash",
            KeyPrefix = "prefix12",
            IsActive = true,
            UserId = "user1",
            LastModifiedBy = "user:test@example.com"
        };

        _apiKeyRepoMock.Setup(r => r.GetByIdAsync(apiKey.Id, default)).ReturnsAsync(apiKey);

        var result = await _service.SetActiveStateAsync(apiKey.Id, false, "other-user", "user:other@example.com");

        Assert.False(result);
        _apiKeyRepoMock.Verify(r => r.Update(It.IsAny<ApiKey>()), Times.Never);
    }
}
