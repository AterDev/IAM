namespace Share.Services;

/// <summary>
/// Interface for password hashing service
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hash a password
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <returns>Hashed password</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verify a password against a hash
    /// </summary>
    /// <param name="hashedPassword">Hashed password</param>
    /// <param name="providedPassword">Plain text password to verify</param>
    /// <returns>True if password matches, false otherwise</returns>
    bool VerifyPassword(string hashedPassword, string providedPassword);

    /// <summary>
    /// Check if password needs rehashing (e.g., algorithm updated)
    /// </summary>
    /// <param name="hashedPassword">Hashed password to check</param>
    /// <returns>True if needs rehashing, false otherwise</returns>
    bool NeedsRehash(string hashedPassword);
}
