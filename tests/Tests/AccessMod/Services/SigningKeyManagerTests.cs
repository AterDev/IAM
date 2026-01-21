using AccessMod.Services;

namespace Tests.AccessMod.Services;

public class SigningKeyManagerTests
{
    [Fact]
    public void SigningKeyManager_Constructor_Succeeds()
    {
        // Arrange
        var mockOAuthService = new Mock<OAuthService>(
            new Mock<DefaultDbContext>(
                new DbContextOptionsBuilder<DefaultDbContext>()
                    .UseInMemoryDatabase("test")
                    .Options
            ).Object,
            new Mock<ILogger<OAuthService>>().Object,
            new MemoryCache(new MemoryCacheOptions())
        );
        var mockLogger = new Mock<ILogger<SigningKeyManager>>();

        // Act
        var manager = new SigningKeyManager(mockOAuthService.Object, mockLogger.Object);

        // Assert
        Assert.NotNull(manager);
    }

    [Fact]
    public async Task RotateIfNeededAsync_WhenThrowsException_Logs()
    {
        // Arrange
        var mockOAuthService = new Mock<OAuthService>(
            new Mock<DefaultDbContext>(
                new DbContextOptionsBuilder<DefaultDbContext>()
                    .UseInMemoryDatabase("test2")
                    .Options
            ).Object,
            new Mock<ILogger<OAuthService>>().Object,
            new MemoryCache(new MemoryCacheOptions())
        );

        mockOAuthService
            .Setup(x => x.IsKeyRotationNeededAsync(It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("Test error"));

        var mockLogger = new Mock<ILogger<SigningKeyManager>>();
        var manager = new SigningKeyManager(mockOAuthService.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.RotateIfNeededAsync());
    }
}
