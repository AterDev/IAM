namespace Entity.Identity;

/// <summary>
/// Role claim entity
/// </summary>
[Module(Modules.Identity)]
public class RoleClaim : EntityBase
{
    /// <summary>
    /// Role ID
    /// </summary>
    public Guid RoleId { get; set; }

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
    /// Role navigation
    /// </summary>
    public Role Role { get; set; } = null!;
}
