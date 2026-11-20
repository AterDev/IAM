namespace Entity.IAMMod;

/// <summary>
/// User role relationship entity
/// </summary>
[Perigon.PostgreSQL.Attributes.Index(nameof(UserRole.UserId), nameof(UserRole.RoleId), IsUnique = true)]
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
