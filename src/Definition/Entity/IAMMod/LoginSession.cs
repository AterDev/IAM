namespace Entity.IAMMod;

/// <summary>
/// Login session entity for tracking user sessions
/// </summary>
public class LoginSession : EntityBase
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Session ID
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(256)")]
    public string SessionId { get; set; }

    /// <summary>
    /// IP address
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(50)")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(500)")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Device info
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(500)")]
    public string? DeviceInfo { get; set; }

    /// <summary>
    /// Login time
    /// </summary>
    public DateTimeOffset LoginTime { get; set; }

    /// <summary>
    /// Last activity time
    /// </summary>
    public DateTimeOffset LastActivityTime { get; set; }

    /// <summary>
    /// Expiration time
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// Is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// User navigation
    /// </summary>
    public User User { get; set; } = null!;
}
