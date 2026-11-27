namespace Entity.IAMMod;

/// <summary>
/// User claim entity
/// </summary>
public class UserClaim : EntityBase
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Claim type
    /// </summary>
    public string ClaimType { get; set; }

    /// <summary>
    /// Claim value
    /// </summary>
    public string? ClaimValue { get; set; }

    /// <summary>
    /// User navigation
    /// </summary>
    public User User { get; set; } = null!;
}
