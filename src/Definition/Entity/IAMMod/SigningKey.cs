namespace Entity.IAMMod;

/// <summary>
/// Signing key entity for JWT key management
/// </summary>
public class SigningKey : EntityBase
{
    /// <summary>
    /// Key identifier
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(256)")]
    public string KeyId { get; set; }

    /// <summary>
    /// Algorithm (e.g., RS256, ES256)
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(50)")]
    public string Algorithm { get; set; }

    /// <summary>
    /// Key type (RSA, EC, etc.)
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(50)")]
    public string KeyType { get; set; }

    /// <summary>
    /// Public key (PEM format)
    /// </summary>
    public string PublicKey { get; set; }

    /// <summary>
    /// Private key (encrypted PEM format)
    /// </summary>
    public string PrivateKey { get; set; }

    /// <summary>
    /// Key usage (signing, encryption)
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(50)")]
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
