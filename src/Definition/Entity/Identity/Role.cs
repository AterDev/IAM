namespace Entity.Identity;

/// <summary>
/// Role entity for identity management
/// </summary>
[Module(Modules.Identity)]
public class Role : EntityBase
{
    /// <summary>
    /// Role name
    /// </summary>
    [MaxLength(256)]
    public required string Name { get; set; }

    /// <summary>
    /// Normalized role name for search
    /// </summary>
    [MaxLength(256)]
    public required string NormalizedName { get; set; }

    /// <summary>
    /// Concurrency stamp
    /// </summary>
    [MaxLength(100)]
    public string? ConcurrencyStamp { get; set; }

    /// <summary>
    /// Role description
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// User roles
    /// </summary>
    public List<UserRole> UserRoles { get; set; } = [];

    /// <summary>
    /// Role claims
    /// </summary>
    public List<RoleClaim> RoleClaims { get; set; } = [];
}
