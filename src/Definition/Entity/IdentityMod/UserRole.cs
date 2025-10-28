namespace Entity.IdentityMod;

/// <summary>
/// User role relationship entity
/// </summary>
[Module(Modules.Identity)]
public class UserRole : EntityBase
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Role ID
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// User navigation
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Role navigation
    /// </summary>
    public Role Role { get; set; } = null!;
}
