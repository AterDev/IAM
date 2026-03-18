using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace IAMMod.Services;

/// <summary>
/// Provides TOTP generation and validation for MFA flows.
/// </summary>
public class MfaTotpService(ILogger<MfaTotpService> logger)
{
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    private const int SecretBytesLength = 20;
    private const int TimeStepSeconds = 30;
    private const int Digits = 6;
    private readonly ILogger<MfaTotpService> _logger = logger;

    public string GenerateSecret()
    {
        var bytes = new byte[SecretBytesLength];
        RandomNumberGenerator.Fill(bytes);
        return EncodeBase32(bytes);
    }

    public string BuildOtpAuthUri(string issuer, string accountName, string secret)
    {
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedAccountName = Uri.EscapeDataString(accountName);
        return $"otpauth://totp/{encodedIssuer}:{encodedAccountName}?secret={secret}&issuer={encodedIssuer}&digits={Digits}&period={TimeStepSeconds}";
    }

    public bool ValidateCode(string secret, string code, DateTimeOffset? now = null, int allowedDriftWindows = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var normalizedCode = NormalizeCode(code);
        if (normalizedCode.Length != Digits || !normalizedCode.All(char.IsDigit))
        {
            return false;
        }

        try
        {
            var secretBytes = DecodeBase32(secret);
            var timestamp = now ?? DateTimeOffset.UtcNow;
            var currentCounter = timestamp.ToUnixTimeSeconds() / TimeStepSeconds;

            for (var offset = -allowedDriftWindows; offset <= allowedDriftWindows; offset++)
            {
                var candidate = GenerateHotp(secretBytes, currentCounter + offset);
                if (candidate == normalizedCode)
                {
                    return true;
                }
            }
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Invalid MFA secret format.");
        }

        return false;
    }

    public static string NormalizeCode(string code)
    {
        return new string(code.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
    }

    private static string GenerateHotp(byte[] secret, long counter)
    {
        Span<byte> counterBytes = stackalloc byte[8];
        for (var i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xff);
            counter >>= 8;
        }

        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(counterBytes.ToArray());
        var offset = hash[^1] & 0x0f;
        var binaryCode = ((hash[offset] & 0x7f) << 24)
            | (hash[offset + 1] << 16)
            | (hash[offset + 2] << 8)
            | hash[offset + 3];

        var otp = binaryCode % (int)Math.Pow(10, Digits);
        return otp.ToString(CultureInfo.InvariantCulture).PadLeft(Digits, '0');
    }

    private static string EncodeBase32(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return string.Empty;
        }

        var output = new StringBuilder((int)Math.Ceiling(data.Length / 5d) * 8);
        var value = 0;
        var bits = 0;

        foreach (var current in data)
        {
            value = (value << 8) | current;
            bits += 8;

            while (bits >= 5)
            {
                output.Append(Base32Alphabet[(value >> (bits - 5)) & 0x1f]);
                bits -= 5;
            }
        }

        if (bits > 0)
        {
            output.Append(Base32Alphabet[(value << (5 - bits)) & 0x1f]);
        }

        return output.ToString();
    }

    private static byte[] DecodeBase32(string input)
    {
        var normalized = input.Trim().TrimEnd('=').Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        if (normalized.Length == 0)
        {
            return [];
        }

        var buffer = new List<byte>(normalized.Length * 5 / 8);
        var value = 0;
        var bits = 0;

        foreach (var c in normalized)
        {
            var index = Base32Alphabet.IndexOf(c);
            if (index < 0)
            {
                throw new FormatException($"Invalid Base32 character '{c}'.");
            }

            value = (value << 5) | index;
            bits += 5;

            if (bits >= 8)
            {
                buffer.Add((byte)((value >> (bits - 8)) & 0xff));
                bits -= 8;
            }
        }

        return [.. buffer];
    }
}
