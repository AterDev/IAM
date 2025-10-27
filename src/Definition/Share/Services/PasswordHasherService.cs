using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Share.Services;

/// <summary>
/// Password hasher service using PBKDF2
/// </summary>
public class PasswordHasherService : IPasswordHasher
{
    private const int SaltSize = 16; // 128 bits
    private const int HashSize = 32; // 256 bits
    private const int Iterations = 100000; // PBKDF2 iterations
    private const char Delimiter = ':';
    private const int CurrentVersion = 1;

    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentNullException(nameof(password));
        }

        // Generate a random salt
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        // Hash the password with PBKDF2
        byte[] hash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: Iterations,
            numBytesRequested: HashSize
        );

        // Format: version:iterations:salt:hash (all base64 encoded)
        return string.Join(Delimiter,
            CurrentVersion,
            Iterations,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash)
        );
    }

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword))
        {
            throw new ArgumentNullException(nameof(hashedPassword));
        }
        if (string.IsNullOrEmpty(providedPassword))
        {
            throw new ArgumentNullException(nameof(providedPassword));
        }

        try
        {
            var parts = hashedPassword.Split(Delimiter);
            if (parts.Length != 4)
            {
                return false;
            }

            var version = int.Parse(parts[0]);
            var iterations = int.Parse(parts[1]);
            var salt = Convert.FromBase64String(parts[2]);
            var hash = Convert.FromBase64String(parts[3]);

            // Hash the provided password with the same salt
            byte[] providedHash = KeyDerivation.Pbkdf2(
                password: providedPassword,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: iterations,
                numBytesRequested: HashSize
            );

            // Compare hashes using constant-time comparison
            return CryptographicOperations.FixedTimeEquals(hash, providedHash);
        }
        catch
        {
            return false;
        }
    }

    public bool NeedsRehash(string hashedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword))
        {
            return true;
        }

        try
        {
            var parts = hashedPassword.Split(Delimiter);
            if (parts.Length != 4)
            {
                return true;
            }

            var version = int.Parse(parts[0]);
            var iterations = int.Parse(parts[1]);

            // Check if version or iteration count has changed
            return version != CurrentVersion || iterations != Iterations;
        }
        catch
        {
            return true;
        }
    }
}
