using System.Text.Json.Serialization;

namespace Entity.IAMMod;

/// <summary>
/// User entity for identity management
/// </summary>
public class User : EntityBase
{
    /// <summary>
    /// User name
    /// </summary>
    [MaxLength(256)]
    public required string UserName { get; set; }

    /// <summary>
    /// Normalized user name for search and filtering
    /// </summary>
    [MaxLength(256)]
    public required string NormalizedUserName { get; set; }

    /// <summary>
    /// Email address
    /// </summary>
    [MaxLength(256)]
    public required string Email { get; set; }

    /// <summary>
    /// Normalized email for case-insensitive unique lookup
    /// </summary>
    [MaxLength(256)]
    public required string NormalizedEmail { get; set; }

    /// <summary>
    /// Email confirmed flag
    /// </summary>
    public bool EmailConfirmed { get; set; }

    /// <summary>
    /// Password hash
    /// </summary>
    [JsonIgnore]
    [MaxLength(100)]
    public string PasswordHash { get; set; } = default!;

    [JsonIgnore]
    [MaxLength(100)]
    public string PasswordSalt { get; set; } = default!;

    /// <summary>
    /// Security stamp for password reset
    /// </summary>
    [MaxLength(100)]
    public string? SecurityStamp { get; set; }

    /// <summary>
    /// Concurrency stamp
    /// </summary>
    [MaxLength(100)]
    public string? ConcurrencyStamp { get; set; }

    /// <summary>
    /// Phone number
    /// </summary>
    [MaxLength(50)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Phone number confirmed flag
    /// </summary>
    public bool PhoneNumberConfirmed { get; set; }

    /// <summary>
    /// Two factor authentication enabled
    /// </summary>
    public bool IsTwoFactorEnabled { get; set; }

    /// <summary>
    /// Lockout end date
    /// </summary>
    public DateTimeOffset? LockoutEnd { get; set; }

    /// <summary>
    /// Lockout enabled flag
    /// </summary>
    public bool LockoutEnabled { get; set; }

    /// <summary>
    /// Failed access count
    /// </summary>
    public int AccessFailedCount { get; set; }

    /// <summary>
    /// User roles
    /// </summary>
    public List<UserRole> UserRoles { get; set; } = [];

    /// <summary>
    /// User claims
    /// </summary>
    public List<UserClaim> UserClaims { get; set; } = [];

    /// <summary>
    /// User logins
    /// </summary>
    public List<UserLogin> UserLogins { get; set; } = [];

    /// <summary>
    /// User tokens
    /// </summary>
    public List<UserToken> UserTokens { get; set; } = [];

    /// <summary>
    /// Organization users
    /// </summary>
    public List<OrganizationUser> OrganizationUsers { get; set; } = [];

    /// <summary>
    /// Login sessions
    /// </summary>
    public List<LoginSession> LoginSessions { get; set; } = [];
}
