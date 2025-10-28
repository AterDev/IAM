using Share.Services;
using Xunit;

namespace Share.Tests.Services;

public class PasswordHasherServiceTests
{
    private readonly IPasswordHasher _passwordHasher;

    public PasswordHasherServiceTests()
    {
        _passwordHasher = new PasswordHasherService();
    }

    [Fact]
    public void HashPassword_ValidPassword_ReturnsHashedPassword()
    {
        // Arrange
        var password = "Test@Password123";

        // Act
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Assert
        Assert.NotNull(hashedPassword);
        Assert.NotEmpty(hashedPassword);
        Assert.Contains(":", hashedPassword);
        
        // Should have 4 parts: version:iterations:salt:hash
        var parts = hashedPassword.Split(':');
        Assert.Equal(4, parts.Length);
    }

    [Fact]
    public void HashPassword_NullPassword_ThrowsArgumentNullException()
    {
        // Arrange
        string? password = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _passwordHasher.HashPassword(password!));
    }

    [Fact]
    public void HashPassword_EmptyPassword_ThrowsArgumentNullException()
    {
        // Arrange
        var password = string.Empty;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _passwordHasher.HashPassword(password));
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "Test@Password123";
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(hashedPassword, password);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_IncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var password = "Test@Password123";
        var wrongPassword = "Wrong@Password456";
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(hashedPassword, wrongPassword);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_NullHashedPassword_ThrowsArgumentNullException()
    {
        // Arrange
        string? hashedPassword = null;
        var password = "Test@Password123";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _passwordHasher.VerifyPassword(hashedPassword!, password));
    }

    [Fact]
    public void VerifyPassword_NullProvidedPassword_ThrowsArgumentNullException()
    {
        // Arrange
        var password = "Test@Password123";
        var hashedPassword = _passwordHasher.HashPassword(password);
        string? providedPassword = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _passwordHasher.VerifyPassword(hashedPassword, providedPassword!));
    }

    [Fact]
    public void VerifyPassword_MalformedHash_ReturnsFalse()
    {
        // Arrange
        var malformedHash = "invalid:hash:format";
        var password = "Test@Password123";

        // Act
        var result = _passwordHasher.VerifyPassword(malformedHash, password);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HashPassword_DifferentPasswords_GenerateDifferentHashes()
    {
        // Arrange
        var password1 = "Test@Password123";
        var password2 = "Test@Password456";

        // Act
        var hash1 = _passwordHasher.HashPassword(password1);
        var hash2 = _passwordHasher.HashPassword(password2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashPassword_SamePassword_GeneratesDifferentHashes()
    {
        // Arrange
        var password = "Test@Password123";

        // Act
        var hash1 = _passwordHasher.HashPassword(password);
        var hash2 = _passwordHasher.HashPassword(password);

        // Assert
        // Even the same password should generate different hashes due to random salt
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void NeedsRehash_ValidHash_ReturnsFalse()
    {
        // Arrange
        var password = "Test@Password123";
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.NeedsRehash(hashedPassword);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void NeedsRehash_NullHash_ReturnsTrue()
    {
        // Arrange
        string? hashedPassword = null;

        // Act
        var result = _passwordHasher.NeedsRehash(hashedPassword!);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void NeedsRehash_EmptyHash_ReturnsTrue()
    {
        // Arrange
        var hashedPassword = string.Empty;

        // Act
        var result = _passwordHasher.NeedsRehash(hashedPassword);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void NeedsRehash_MalformedHash_ReturnsTrue()
    {
        // Arrange
        var hashedPassword = "invalid:hash";

        // Act
        var result = _passwordHasher.NeedsRehash(hashedPassword);

        // Assert
        Assert.True(result);
    }
}
