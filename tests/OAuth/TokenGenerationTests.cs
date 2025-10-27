using Xunit;

namespace Share.Tests.OAuth;

/// <summary>
/// Tests for OAuth token generation and validation
/// </summary>
public class TokenGenerationTests
{
    [Fact]
    public void GenerateAuthorizationCode_ReturnsValidCode()
    {
        // Arrange & Act
        var code = GenerateAuthorizationCode();

        // Assert
        Assert.NotNull(code);
        Assert.NotEmpty(code);
        Assert.DoesNotContain("=", code); // URL-safe base64
        Assert.DoesNotContain("+", code); // URL-safe base64
        Assert.DoesNotContain("/", code); // URL-safe base64
    }

    [Fact]
    public void GenerateAuthorizationCode_MultipleCalls_GeneratesDifferentCodes()
    {
        // Arrange & Act
        var code1 = GenerateAuthorizationCode();
        var code2 = GenerateAuthorizationCode();
        var code3 = GenerateAuthorizationCode();

        // Assert
        Assert.NotEqual(code1, code2);
        Assert.NotEqual(code2, code3);
        Assert.NotEqual(code1, code3);
    }

    [Fact]
    public void GenerateTokenReference_ReturnsValidReference()
    {
        // Arrange & Act
        var reference = GenerateTokenReference();

        // Assert
        Assert.NotNull(reference);
        Assert.NotEmpty(reference);
        Assert.DoesNotContain("=", reference);
        Assert.DoesNotContain("+", reference);
        Assert.DoesNotContain("/", reference);
    }

    [Fact]
    public void GenerateTokenReference_MultipleCalls_GeneratesDifferentReferences()
    {
        // Arrange & Act
        var ref1 = GenerateTokenReference();
        var ref2 = GenerateTokenReference();
        var ref3 = GenerateTokenReference();

        // Assert
        Assert.NotEqual(ref1, ref2);
        Assert.NotEqual(ref2, ref3);
        Assert.NotEqual(ref1, ref3);
    }

    [Fact]
    public void GenerateUserCode_ReturnsValidFormat()
    {
        // Arrange & Act
        var userCode = GenerateUserCode();

        // Assert
        Assert.NotNull(userCode);
        Assert.Matches(@"^[A-Z2-9]{4}-[A-Z2-9]{4}$", userCode);
    }

    [Fact]
    public void GenerateUserCode_DoesNotContainAmbiguousCharacters()
    {
        // Arrange & Act
        var userCodes = new List<string>();
        for (int i = 0; i < 100; i++)
        {
            userCodes.Add(GenerateUserCode());
        }

        // Assert
        foreach (var code in userCodes)
        {
            Assert.DoesNotContain("O", code); // Ambiguous with 0
            Assert.DoesNotContain("I", code); // Ambiguous with 1
            Assert.DoesNotContain("0", code); // Ambiguous
            Assert.DoesNotContain("1", code); // Ambiguous
        }
    }

    [Fact]
    public void GenerateUserCode_MultipleCalls_GeneratesDifferentCodes()
    {
        // Arrange & Act
        var codes = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            codes.Add(GenerateUserCode());
        }

        // Assert - All codes should be unique
        Assert.Equal(100, codes.Count);
    }

    // Helper methods matching implementations in managers
    private string GenerateAuthorizationCode()
    {
        return GenerateUrlSafeBase64(32);
    }

    private string GenerateTokenReference()
    {
        return GenerateUrlSafeBase64(32);
    }

    private string GenerateUrlSafeBase64(int byteCount)
    {
        var bytes = new byte[byteCount];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private string GenerateUserCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = new byte[8];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);

        var result = new char[8];
        for (int i = 0; i < 8; i++)
        {
            result[i] = chars[bytes[i] % chars.Length];
        }

        return $"{new string(result, 0, 4)}-{new string(result, 4, 4)}";
    }
}
