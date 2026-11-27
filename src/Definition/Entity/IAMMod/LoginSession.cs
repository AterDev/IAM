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
    public string SessionId { get; set; }

    /// <summary>
    /// IP address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Device info
    /// </summary>
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
