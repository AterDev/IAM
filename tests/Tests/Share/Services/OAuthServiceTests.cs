namespace Tests.IAMMod.Services;

/// <summary>
/// OAuthService 当前实现的基础功能测试
/// </summary>
public class OAuthServiceTests
{
    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Issuer"] = "https://issuer.example.com",
        })
        .Build();

    private readonly OAuthService _service = new(
        NullLogger<OAuthService>.Instance,
        Options.Create(
            new JwtOption
            {
                ValidAudiences = "iam-tests",
                Sign = "unused",
            }
        ),
        Configuration
    );

    [Fact]
    public void Constructor_InitializesSuccessfully()
    {
        Assert.NotNull(_service);
    }

    [Fact]
    public void GenerateToken_WithNullClaims_ThrowsArgumentNullException()
    {
        var signingKey = CreateSigningKey();
        Assert.Throws<ArgumentNullException>(() => _service.GenerateToken(null!, signingKey));
    }

    [Fact]
    public void GenerateToken_WithZeroExpiration_ThrowsArgumentOutOfRangeException()
    {
        var signingKey = CreateSigningKey();
        var claims = new[] { new Claim("sub", "user123") };

        Assert.Throws<ArgumentOutOfRangeException>(() => _service.GenerateToken(claims, signingKey, 0));
    }

    [Fact]
    public void GenerateTokenReference_ReturnsNonEmptyString()
    {
        var reference = OAuthService.GenerateTokenReference();

        Assert.NotEmpty(reference);
        Assert.True(reference.Length > 32);
    }

    [Fact]
    public void ValidatePkce_WithPlainMethod_ValidatesCorrectly()
    {
        var result = OAuthService.ValidatePkce("test-verifier-123", "test-verifier-123", "plain");

        Assert.True(result);
    }

    [Fact]
    public void ValidatePkce_WithPlainMethod_RejectsMismatch()
    {
        var result = OAuthService.ValidatePkce("test-verifier-123", "different-challenge", "plain");

        Assert.False(result);
    }

    [Fact]
    public void ValidatePkce_WithS256Method_ValidatesCorrectly()
    {
        var verifier = "abcdefghijklmnopqrstuvwxyz123456";
        var sha256Hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(verifier));
        var challenge = Convert.ToBase64String(sha256Hash)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var result = OAuthService.ValidatePkce(verifier, challenge, "S256");

        Assert.True(result);
    }

    [Fact]
    public void ValidatePkce_WithInvalidMethod_ReturnsFalse()
    {
        var result = OAuthService.ValidatePkce("verifier", "challenge", "invalid");

        Assert.False(result);
    }

    private static SigningKey CreateSigningKey()
    {
        var (publicKey, privateKey) = HashCrypto.GenerateRsaKeyPair(2048);

        return new SigningKey
        {
            KeyId = Guid.NewGuid().ToString("N"),
            Algorithm = "RS256",
            KeyType = "RSA",
            PublicKey = publicKey,
            PrivateKey = privateKey,
            Usage = "signing",
            ActivationDate = DateTimeOffset.UtcNow.AddMinutes(-1),
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(1),
            IsActive = true,
        };
    }
}
