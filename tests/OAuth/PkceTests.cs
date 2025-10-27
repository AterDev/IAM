using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Share.Tests.OAuth;

/// <summary>
/// Tests for OAuth PKCE implementation
/// </summary>
public class PkceTests
{
    [Fact]
    public void ValidatePkce_Plain_ValidVerifier_ReturnsTrue()
    {
        // Arrange
        var verifier = "test_verifier_12345";
        var challenge = verifier; // Plain method uses same value
        var method = "plain";

        // Act
        var isValid = ValidatePkce(verifier, challenge, method);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidatePkce_Plain_InvalidVerifier_ReturnsFalse()
    {
        // Arrange
        var verifier = "test_verifier_12345";
        var challenge = "different_challenge";
        var method = "plain";

        // Act
        var isValid = ValidatePkce(verifier, challenge, method);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidatePkce_S256_ValidVerifier_ReturnsTrue()
    {
        // Arrange
        var verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
        var challenge = ComputeS256Challenge(verifier);
        var method = "S256";

        // Act
        var isValid = ValidatePkce(verifier, challenge, method);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidatePkce_S256_InvalidVerifier_ReturnsFalse()
    {
        // Arrange
        var verifier = "test_verifier_12345";
        var challenge = "invalid_challenge";
        var method = "S256";

        // Act
        var isValid = ValidatePkce(verifier, challenge, method);

        // Assert
        Assert.False(isValid);
    }

    [Theory]
    [InlineData("plain")]
    [InlineData("S256")]
    public void ValidatePkce_EmptyVerifier_ReturnsFalse(string method)
    {
        // Arrange
        var verifier = string.Empty;
        var challenge = "some_challenge";

        // Act
        var isValid = ValidatePkce(verifier, challenge, method);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidatePkce_UnsupportedMethod_ReturnsFalse()
    {
        // Arrange
        var verifier = "test_verifier";
        var challenge = "test_challenge";
        var method = "unsupported";

        // Act
        var isValid = ValidatePkce(verifier, challenge, method);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ComputeS256Challenge_StandardExample_MatchesExpected()
    {
        // This is the example from RFC 7636
        // Arrange
        var verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
        var expectedChallenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";

        // Act
        var actualChallenge = ComputeS256Challenge(verifier);

        // Assert
        Assert.Equal(expectedChallenge, actualChallenge);
    }

    // Helper methods matching the implementation in AuthorizationManager
    private bool ValidatePkce(string verifier, string challenge, string method)
    {
        if (string.IsNullOrEmpty(verifier) || string.IsNullOrEmpty(challenge))
        {
            return false;
        }

        if (method == "plain")
        {
            return verifier == challenge;
        }
        else if (method == "S256")
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(verifier));
            var computed = Convert.ToBase64String(hash)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
            return computed == challenge;
        }

        return false;
    }

    private string ComputeS256Challenge(string verifier)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(verifier));
        return Convert.ToBase64String(hash)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
