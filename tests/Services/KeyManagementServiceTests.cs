using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Share.Services;
using Xunit;

namespace Share.Tests.Services;

public class KeyManagementServiceTests
{
    private readonly IKeyManagementService _keyManagementService;

    public KeyManagementServiceTests()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var loggerMock = new Mock<ILogger<KeyManagementService>>();
        _keyManagementService = new KeyManagementService(cache, loggerMock.Object);
    }

    [Fact]
    public void GetSigningCredentials_ReturnsValidCredentials()
    {
        // Act
        var credentials = _keyManagementService.GetSigningCredentials();

        // Assert
        Assert.NotNull(credentials);
        Assert.NotNull(credentials.Key);
        Assert.NotNull(credentials.Algorithm);
        Assert.Equal("RS256", credentials.Algorithm);
    }

    [Fact]
    public void GetTokenValidationParameters_ReturnsValidParameters()
    {
        // Act
        var parameters = _keyManagementService.GetTokenValidationParameters();

        // Assert
        Assert.NotNull(parameters);
        Assert.NotNull(parameters.IssuerSigningKey);
        Assert.True(parameters.ValidateIssuerSigningKey);
        Assert.True(parameters.ValidateLifetime);
    }

    [Fact]
    public void GetCurrentKeyId_ReturnsKeyId()
    {
        // Act
        var keyId = _keyManagementService.GetCurrentKeyId();

        // Assert
        Assert.NotNull(keyId);
        Assert.NotEmpty(keyId);
    }

    [Fact]
    public async Task RotateKeyAsync_GeneratesNewKeyId()
    {
        // Arrange
        var originalKeyId = _keyManagementService.GetCurrentKeyId();

        // Act
        var newKeyId = await _keyManagementService.RotateKeyAsync();

        // Assert
        Assert.NotNull(newKeyId);
        Assert.NotEmpty(newKeyId);
        Assert.NotEqual(originalKeyId, newKeyId);
    }

    [Fact]
    public async Task RotateKeyAsync_UpdatesSigningCredentials()
    {
        // Arrange
        var originalCredentials = _keyManagementService.GetSigningCredentials();
        var originalKeyId = originalCredentials.Key.KeyId;

        // Act
        await _keyManagementService.RotateKeyAsync();
        var newCredentials = _keyManagementService.GetSigningCredentials();

        // Assert
        Assert.NotNull(newCredentials);
        Assert.NotEqual(originalKeyId, newCredentials.Key.KeyId);
    }

    [Fact]
    public async Task GetPublicKeyJwkAsync_ReturnsValidJwk()
    {
        // Act
        var jwk = await _keyManagementService.GetPublicKeyJwkAsync();

        // Assert
        Assert.NotNull(jwk);
        Assert.NotEmpty(jwk);
        
        // Verify JWK structure
        Assert.Contains("\"kty\"", jwk);
        Assert.Contains("\"RSA\"", jwk);
        Assert.Contains("\"use\"", jwk);
        Assert.Contains("\"sig\"", jwk);
        Assert.Contains("\"kid\"", jwk);
        Assert.Contains("\"n\"", jwk);
        Assert.Contains("\"e\"", jwk);
    }

    [Fact]
    public async Task GetPublicKeyJwkAsync_WithValidKeyId_ReturnsJwk()
    {
        // Arrange
        var currentKeyId = _keyManagementService.GetCurrentKeyId();

        // Act
        var jwk = await _keyManagementService.GetPublicKeyJwkAsync(currentKeyId);

        // Assert
        Assert.NotNull(jwk);
        Assert.Contains(currentKeyId!, jwk);
    }

    [Fact]
    public async Task GetPublicKeyJwkAsync_WithInvalidKeyId_ReturnsNull()
    {
        // Arrange
        var invalidKeyId = "invalid-key-id";

        // Act
        var jwk = await _keyManagementService.GetPublicKeyJwkAsync(invalidKeyId);

        // Assert
        Assert.Null(jwk);
    }

    [Fact]
    public void GetSigningCredentials_MultipleCalls_ReturnsSameCredentials()
    {
        // Act
        var credentials1 = _keyManagementService.GetSigningCredentials();
        var credentials2 = _keyManagementService.GetSigningCredentials();

        // Assert
        Assert.Equal(credentials1.Key.KeyId, credentials2.Key.KeyId);
    }

    [Fact]
    public async Task RotateKeyAsync_MultipleCalls_GeneratesDifferentKeys()
    {
        // Act
        var keyId1 = await _keyManagementService.RotateKeyAsync();
        var keyId2 = await _keyManagementService.RotateKeyAsync();

        // Assert
        Assert.NotEqual(keyId1, keyId2);
    }

    [Fact]
    public void GetTokenValidationParameters_ContainsSigningKey()
    {
        // Act
        var parameters = _keyManagementService.GetTokenValidationParameters();

        // Assert
        Assert.NotNull(parameters.IssuerSigningKey);
        Assert.NotNull(parameters.IssuerSigningKey.KeyId);
    }

    [Fact]
    public async Task GetCurrentKeyId_AfterRotation_ReturnsNewKeyId()
    {
        // Arrange
        var originalKeyId = _keyManagementService.GetCurrentKeyId();

        // Act
        var newKeyId = await _keyManagementService.RotateKeyAsync();
        var currentKeyId = _keyManagementService.GetCurrentKeyId();

        // Assert
        Assert.Equal(newKeyId, currentKeyId);
        Assert.NotEqual(originalKeyId, currentKeyId);
    }
}
