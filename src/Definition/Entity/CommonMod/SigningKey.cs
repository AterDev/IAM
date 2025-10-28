namespace Entity.CommonMod;

/// <summary>
/// Signing key entity for JWT key management
/// </summary>
[Module(Modules.Common)]
public class SigningKey : EntityBase
{
    /// <summary>
    /// Key identifier
    /// </summary>
    [MaxLength(256)]
    public required string KeyId { get; set; }

    /// <summary>
    /// Algorithm (e.g., RS256, ES256)
    /// </summary>
    [MaxLength(50)]
    public required string Algorithm { get; set; }

    /// <summary>
    /// Key type (RSA, EC, etc.)
    /// </summary>
    [MaxLength(50)]
    public required string KeyType { get; set; }

    /// <summary>
    /// Public key (PEM format)
    /// </summary>
    public required string PublicKey { get; set; }

    /// <summary>
    /// Private key (encrypted PEM format)
    /// </summary>
    public required string PrivateKey { get; set; }

    /// <summary>
    /// Key usage (signing, encryption)
    /// </summary>
    [MaxLength(50)]
    public string? Usage { get; set; }

    /// <summary>
    /// Activation date
    /// </summary>
    public DateTimeOffset ActivationDate { get; set; }

    /// <summary>
    /// Expiration date
    /// </summary>
    public DateTimeOffset? ExpirationDate { get; set; }

    /// <summary>
    /// Whether the key is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether the key is revoked
    /// </summary>
    public bool IsRevoked { get; set; }
}
