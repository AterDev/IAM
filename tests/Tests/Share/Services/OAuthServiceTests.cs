using Share.Services;
using EntityFramework.AppDbContext;

namespace Tests.Share.Services;

/// <summary>
/// OAuthService 基础功能测试
/// </summary>
public class OAuthServiceTests
{
    private readonly Mock<DefaultDbContext> _mockDbContext;
    private readonly Mock<ILogger<OAuthService>> _mockLogger;
    private readonly IMemoryCache _memoryCache;
    private readonly OAuthService _service;

    public OAuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<DefaultDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;

        _mockDbContext = new Mock<DefaultDbContext>(options);
        _mockLogger = new Mock<ILogger<OAuthService>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _service = new OAuthService(_mockDbContext.Object, _mockLogger.Object, _memoryCache);
    }

    [Fact]
    public void OAuthService_Constructor_InitializesSuccessfully()
    {
        // Assert - 如果构造函数成功运行且没有异常，测试通过
        Assert.NotNull(_service);
    }

    [Fact]
    public async Task GenerateTokenAsync_WithNullClaims_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.GenerateTokenAsync(null!)
        );
    }

    [Fact]
    public async Task GenerateTokenAsync_WithZeroExpiration_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var claims = new[] { new Claim("sub", "user123") };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.GenerateTokenAsync(claims, expiresInSeconds: 0)
        );
    }

    [Fact]
    public async Task GenerateTokenAsync_WithExcessiveExpiration_ThrowsArgumentException()
    {
        // Arrange
        var claims = new[] { new Claim("sub", "user123") };
        const int excessiveSeconds = 365 * 24 * 3600 + 1;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.GenerateTokenAsync(claims, expiresInSeconds: excessiveSeconds)
        );
        Assert.Contains("cannot exceed", ex.Message);
    }

    [Fact]
    public async Task GenerateTokenAsync_WithNoActiveKey_ThrowsInvalidOperationException()
    {
        // Arrange - 数据库中没有密钥
        var claims = new[] { new Claim("sub", "user123") };

        var signingKeysDbSet = new Mock<DbSet<SigningKey>>();
        signingKeysDbSet.As<IQueryable<SigningKey>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider(Enumerable.Empty<SigningKey>().AsQueryable().Provider));
        signingKeysDbSet.As<IQueryable<SigningKey>>()
            .Setup(m => m.Expression)
            .Returns(Enumerable.Empty<SigningKey>().AsQueryable().Expression);
        signingKeysDbSet.As<IQueryable<SigningKey>>()
            .Setup(m => m.ElementType)
            .Returns(typeof(SigningKey));
        signingKeysDbSet.As<IQueryable<SigningKey>>()
            .Setup(m => m.GetEnumerator())
            .Returns(Enumerable.Empty<SigningKey>().GetEnumerator());

        _mockDbContext.Setup(x => x.SigningKeys).Returns(signingKeysDbSet.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GenerateTokenAsync(claims)
        );
        Assert.Contains("No active signing key", ex.Message);
    }

    [Fact]
    public void OAuthService_GenerateTokenReference_ReturnsNonEmptyString()
    {
        // Act
        var reference = OAuthService.GenerateTokenReference();

        // Assert
        Assert.NotEmpty(reference);
        Assert.True(reference.Length > 32);
    }

    [Fact]
    public void OAuthService_ValidatePkce_WithPlainMethod_ValidatesCorrectly()
    {
        // Arrange
        var verifier = "test-verifier-123";
        var challenge = "test-verifier-123";

        // Act
        var result = OAuthService.ValidatePkce(verifier, challenge, "plain");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void OAuthService_ValidatePkce_WithPlainMethod_RejectsMismatch()
    {
        // Arrange
        var verifier = "test-verifier-123";
        var challenge = "different-challenge";

        // Act
        var result = OAuthService.ValidatePkce(verifier, challenge, "plain");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void OAuthService_ValidatePkce_WithS256Method_ValidatesCorrectly()
    {
        // Arrange
        var verifier = "abcdefghijklmnopqrstuvwxyz123456";  // 32 字符
        // S256 challenge 应该是 SHA256(verifier) 的 Base64-URL 编码
        var sha256Hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(verifier)
        );
        var challenge = Base64UrlEncoder.Encode(sha256Hash);

        // Act
        var result = OAuthService.ValidatePkce(verifier, challenge, "S256");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void OAuthService_ValidatePkce_WithInvalidMethod_ReturnsFalse()
    {
        // Act
        var result = OAuthService.ValidatePkce("verifier", "challenge", "invalid");

        // Assert
        Assert.False(result);
    }
}

// 简单的异步查询提供程序用于测试
public class TestAsyncQueryProvider : IQueryProvider
{
    private readonly IQueryProvider _innerProvider;

    public TestAsyncQueryProvider(IQueryProvider innerProvider)
    {
        _innerProvider = innerProvider;
    }

    public IQueryable CreateQuery(Expression expression) => _innerProvider.CreateQuery(expression);
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => _innerProvider.CreateQuery<TElement>(expression);
    public object? Execute(Expression expression) => _innerProvider.Execute(expression);
    public TResult Execute<TResult>(Expression expression) => _innerProvider.Execute<TResult>(expression);
}
