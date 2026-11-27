namespace Entity.IAMMod;

/// <summary>
/// Historical client secret record used for rotation and audit.
/// </summary>
public class ClientSecret : EntityBase
{
    /// <summary>
    /// Client id.
    /// </summary>
    public Guid ClientId { get; set; }

    /// <summary>
    /// Secret hash.
    /// </summary>
    public string SecretHash { get; set; }

    /// <summary>
    /// Secret salt.
    /// </summary>
    public string SecretSalt { get; set; }

    /// <summary>
    /// Last four characters of the plain secret for UI identification.
    /// </summary>
    public string LastFour { get; set; }

    /// <summary>
    /// Secret expiration time.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Revocation time if the secret is no longer valid.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    /// Client navigation property.
    /// </summary>
    public Client Client { get; set; } = null!;
}
