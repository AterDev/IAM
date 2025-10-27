using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Share.Services;
using Xunit;

namespace Share.Tests.Services;

public class JwtTokenServiceTests
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IKeyManagementService _keyManagementService;
    private readonly IConfiguration _configuration;

    public JwtTokenServiceTests()
    {
        // Setup configuration
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "IAM-Test",
            ["Jwt:Audience"] = "IAM-Test-Audience"
        });
        _configuration = configBuilder.Build();

        // Setup key management service
        var cache = new MemoryCache(new MemoryCacheOptions());
        var loggerMock = new Mock<ILogger<KeyManagementService>>();
        _keyManagementService = new KeyManagementService(cache, loggerMock.Object);

        // Setup JWT token service
        _jwtTokenService = new JwtTokenService(_configuration, _keyManagementService);
    }

    [Fact]
    public void GenerateAccessToken_ValidClaims_ReturnsToken()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user123"),
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Email, "test@example.com")
        };

        // Act
        var token = _jwtTokenService.GenerateAccessToken(claims);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        
        // JWT tokens have 3 parts separated by dots
        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);
    }

    [Fact]
    public void GenerateAccessToken_CustomExpiry_ReturnsToken()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user123")
        };
        var expiresIn = 7200; // 2 hours

        // Act
        var token = _jwtTokenService.GenerateAccessToken(claims, expiresIn);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateIdToken_ValidClaims_ReturnsToken()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user123"),
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim("sub", "user123")
        };

        // Act
        var token = _jwtTokenService.GenerateIdToken(claims);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        
        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);
    }

    [Fact]
    public void ValidateToken_ValidToken_ReturnsPrincipal()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user123"),
            new Claim(ClaimTypes.Name, "testuser")
        };
        var token = _jwtTokenService.GenerateAccessToken(claims);

        // Act
        var principal = _jwtTokenService.ValidateToken(token);

        // Assert
        Assert.NotNull(principal);
        Assert.NotNull(principal.Identity);
        Assert.True(principal.Identity.IsAuthenticated);
    }

    [Fact]
    public void ValidateToken_NullToken_ReturnsNull()
    {
        // Arrange
        string? token = null;

        // Act
        var principal = _jwtTokenService.ValidateToken(token!);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_EmptyToken_ReturnsNull()
    {
        // Arrange
        var token = string.Empty;

        // Act
        var principal = _jwtTokenService.ValidateToken(token);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_InvalidToken_ReturnsNull()
    {
        // Arrange
        var token = "invalid.token.here";

        // Act
        var principal = _jwtTokenService.ValidateToken(token);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void GetTokenClaims_ValidToken_ReturnsClaims()
    {
        // Arrange
        var expectedClaims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user123"),
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Email, "test@example.com")
        };
        var token = _jwtTokenService.GenerateAccessToken(expectedClaims);

        // Act
        var claims = _jwtTokenService.GetTokenClaims(token);

        // Assert
        Assert.NotNull(claims);
        Assert.NotEmpty(claims);
        
        var claimsList = claims.ToList();
        Assert.Contains(claimsList, c => c.Type == ClaimTypes.NameIdentifier && c.Value == "user123");
        Assert.Contains(claimsList, c => c.Type == ClaimTypes.Name && c.Value == "testuser");
        Assert.Contains(claimsList, c => c.Type == ClaimTypes.Email && c.Value == "test@example.com");
    }

    [Fact]
    public void GetTokenClaims_NullToken_ReturnsNull()
    {
        // Arrange
        string? token = null;

        // Act
        var claims = _jwtTokenService.GetTokenClaims(token!);

        // Assert
        Assert.Null(claims);
    }

    [Fact]
    public void GetTokenClaims_EmptyToken_ReturnsNull()
    {
        // Arrange
        var token = string.Empty;

        // Act
        var claims = _jwtTokenService.GetTokenClaims(token);

        // Assert
        Assert.Null(claims);
    }

    [Fact]
    public void GetTokenClaims_InvalidToken_ReturnsNull()
    {
        // Arrange
        var token = "invalid.token.here";

        // Act
        var claims = _jwtTokenService.GetTokenClaims(token);

        // Assert
        Assert.Null(claims);
    }

    [Fact]
    public void GenerateAccessToken_MultipleCalls_GeneratesDifferentTokens()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user123")
        };

        // Act
        var token1 = _jwtTokenService.GenerateAccessToken(claims);
        System.Threading.Thread.Sleep(1000); // Ensure different timestamp
        var token2 = _jwtTokenService.GenerateAccessToken(claims);

        // Assert
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void ValidateToken_ExpiredToken_ReturnsNull()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user123")
        };
        var token = _jwtTokenService.GenerateAccessToken(claims, -1); // Already expired

        // Act
        System.Threading.Thread.Sleep(1000); // Wait for expiry
        var principal = _jwtTokenService.ValidateToken(token);

        // Assert
        Assert.Null(principal);
    }
}
