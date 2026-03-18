using Perigon.AspNetCore.Utils;

namespace Tests.Perigon.AspNetCore.Utils;

public class HashCryptoTests
{
    #region RSA Key Generation Tests

    [Fact]
    public void GenerateRsaKeyPair_WithDefault2048_GeneratesValidPair()
    {
        // Act
        var (publicKey, privateKey) = HashCrypto.GenerateRsaKeyPair();

        // Assert
        Assert.NotEmpty(publicKey);
        Assert.NotEmpty(privateKey);
        Assert.True(publicKey.StartsWith("MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8A") || publicKey.Contains("BEGIN"));
    }

    [Fact]
    public void GenerateRsaKeyPair_With4096_GeneratesLargerKey()
    {
        // Act
        var (publicKey, privateKey) = HashCrypto.GenerateRsaKeyPair(4096);

        // Assert
        Assert.NotEmpty(publicKey);
        Assert.NotEmpty(privateKey);
        Assert.True(privateKey.Length > 1000);  // 4096 位密钥应该更大
    }

    #endregion

    #region RSA Signature Tests

    [Fact]
    public void SignWithRsa_ValidInput_GeneratesSignature()
    {
        // Arrange
        var (publicKey, privateKey) = HashCrypto.GenerateRsaKeyPair();
        var data = "test data to sign";

        // Act
        var signature = HashCrypto.SignWithRsa(data, privateKey);

        // Assert
        Assert.NotEmpty(signature);
    }

    [Fact]
    public void SignWithRsa_NullData_ThrowsArgumentNullException()
    {
        // Arrange
        var (_, privateKey) = HashCrypto.GenerateRsaKeyPair();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            HashCrypto.SignWithRsa(null!, privateKey)
        );
    }

    [Fact]
    public void SignWithRsa_NullPrivateKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            HashCrypto.SignWithRsa("test data", null!)
        );
    }

    #endregion

    #region RSA Verification Tests

    [Fact]
    public void VerifyWithRsa_ValidSignature_ReturnsTrue()
    {
        // Arrange
        var (publicKey, privateKey) = HashCrypto.GenerateRsaKeyPair();
        var data = "test data";
        var signature = HashCrypto.SignWithRsa(data, privateKey);

        // Act
        var result = HashCrypto.VerifyWithRsa(data, signature, publicKey);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyWithRsa_InvalidSignature_ReturnsFalse()
    {
        // Arrange
        var (publicKey, privateKey) = HashCrypto.GenerateRsaKeyPair();
        var data = "test data";
        var signature = HashCrypto.SignWithRsa(data, privateKey);
        var invalidSignature = signature[..^4] + "XXXX";  // 修改签名

        // Act
        var result = HashCrypto.VerifyWithRsa(data, invalidSignature, publicKey);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyWithRsa_NullData_ReturnsFalse()
    {
        // Arrange
        var (publicKey, _) = HashCrypto.GenerateRsaKeyPair();

        // Act
        var result = HashCrypto.VerifyWithRsa(null!, "signature", publicKey);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyWithRsa_InvalidBase64Signature_ReturnsFalse()
    {
        // Arrange
        var (publicKey, _) = HashCrypto.GenerateRsaKeyPair();

        // Act
        var result = HashCrypto.VerifyWithRsa("data", "not-base64!!!", publicKey);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyWithRsa_EmptySignature_ReturnsFalse()
    {
        // Arrange
        var (publicKey, _) = HashCrypto.GenerateRsaKeyPair();

        // Act
        var result = HashCrypto.VerifyWithRsa("data", string.Empty, publicKey);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region RSA Key Import Tests

    [Fact]
    public void ImportRsaPublicKey_ValidKey_Succeeds()
    {
        // Arrange
        var (publicKey, _) = HashCrypto.GenerateRsaKeyPair();

        // Act
        using var rsa = HashCrypto.ImportRsaPublicKey(publicKey);

        // Assert
        Assert.NotNull(rsa);
        Assert.True(rsa.KeySize >= 2048);
    }

    [Fact]
    public void ImportRsaPrivateKey_ValidKey_Succeeds()
    {
        // Arrange
        var (_, privateKey) = HashCrypto.GenerateRsaKeyPair();

        // Act
        using var rsa = HashCrypto.ImportRsaPrivateKey(privateKey);

        // Assert
        Assert.NotNull(rsa);
        Assert.True(rsa.KeySize >= 2048);
    }

    [Fact]
    public void ImportRsaPrivateKey_InvalidKey_ThrowsFormatOrCryptographicException()
    {
        // Act & Assert
        var exception = Record.Exception(() => HashCrypto.ImportRsaPrivateKey("invalid-key-data"));

        Assert.NotNull(exception);
        Assert.True(
            exception is FormatException or CryptographicException,
            $"Unexpected exception type: {exception.GetType().FullName}"
        );
    }

    #endregion

    #region JWK Components Tests

    [Fact]
    public void ExtractRsaJwkComponents_ValidKey_ReturnsComponents()
    {
        // Arrange
        var (publicKey, _) = HashCrypto.GenerateRsaKeyPair();

        // Act
        var (n, e) = HashCrypto.ExtractRsaJwkComponents(publicKey);

        // Assert
        Assert.NotEmpty(n);
        Assert.NotEmpty(e);
        Assert.Equal("AQAB", e);  // 公钥指数通常是 65537
    }

    #endregion
}
