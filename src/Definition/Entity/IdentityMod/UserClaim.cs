namespace Entity.IdentityMod;

/// <summary>
/// User claim entity
/// </summary>
[Module(Modules.Identity)]
public class UserClaim : EntityBase
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Claim type
    /// </summary>
    [MaxLength(256)]
    public required string ClaimType { get; set; }

    /// <summary>
    /// Claim value
    /// </summary>
    [MaxLength(500)]
    public string? ClaimValue { get; set; }

    /// <summary>
    /// User navigation
    /// </summary>
    public User User { get; set; } = null!;
}
