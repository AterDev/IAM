namespace Tests.IAMMod.Services;

public class MfaTotpServiceTests
{
    private readonly MfaTotpService _service = new(NullLogger<MfaTotpService>.Instance);

    [Fact]
    public void GenerateSecret_ReturnsBase32SecretWithExpectedLength()
    {
        var secret = _service.GenerateSecret();

        Assert.Equal(32, secret.Length);
        Assert.All(secret, c => Assert.Contains(c, "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"));
    }

    [Fact]
    public void BuildOtpAuthUri_EncodesIssuerAndAccountName()
    {
        var uri = _service.BuildOtpAuthUri("IAM Center", "alice@example.com", "JBSWY3DPEHPK3PXP");

        Assert.Equal(
            "otpauth://totp/IAM%20Center:alice%40example.com?secret=JBSWY3DPEHPK3PXP&issuer=IAM%20Center&digits=6&period=30",
            uri);
    }

    [Fact]
    public void ValidateCode_WithKnownRfcVector_ReturnsTrue()
    {
        const string secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";
        var timestamp = DateTimeOffset.FromUnixTimeSeconds(59);

        var result = _service.ValidateCode(secret, "287082", timestamp, allowedDriftWindows: 0);

        Assert.True(result);
    }

    [Theory]
    [InlineData("123 456", "123456")]
    [InlineData("ab-cd-12", "ABCD12")]
    [InlineData("  a b c  ", "ABC")]
    public void NormalizeCode_RemovesSeparatorsAndUppercases(string input, string expected)
    {
        var normalized = MfaTotpService.NormalizeCode(input);

        Assert.Equal(expected, normalized);
    }
}
