namespace Entity.IAMMod;

/// <summary>
/// Token entity for OAuth/OIDC tokens
/// </summary>
public class Token : EntityBase
{
    /// <summary>
    /// Authorization ID
    /// </summary>
    public Guid? AuthorizationId { get; set; }

    /// <summary>
    /// Reference identifier
    /// </summary>
    public string? ReferenceId { get; set; }

    /// <summary>
    /// Token type (access_token, refresh_token, id_token, device_code, user_code)
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Status (valid, revoked, redeemed)
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Subject identifier
    /// </summary>
    public string? SubjectId { get; set; }

    /// <summary>
    /// Payload (encrypted token or claims)
    /// </summary>
    public string? Payload { get; set; }

    /// <summary>
    /// Properties (JSON object)
    /// </summary>
    public string? Properties { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTimeOffset CreationDate { get; set; }

    /// <summary>
    /// Expiration date
    /// </summary>
    public DateTimeOffset? ExpirationDate { get; set; }

    /// <summary>
    /// Redemption date
    /// </summary>
    public DateTimeOffset? RedemptionDate { get; set; }

    /// <summary>
    /// Authorization navigation
    /// </summary>
    public Authorization? Authorization { get; set; }
}
