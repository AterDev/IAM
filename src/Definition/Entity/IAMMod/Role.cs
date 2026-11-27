namespace Entity.IAMMod;

/// <summary>
/// Role entity for identity management
/// </summary>
public class Role : EntityBase
{
    /// <summary>
    /// Role name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Normalized role name for search
    /// </summary>
    public string NormalizedName { get; set; }

    /// <summary>
    /// Concurrency stamp
    /// </summary>
    public string? ConcurrencyStamp { get; set; }

    /// <summary>
    /// Role description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// User roles
    /// </summary>
    public List<UserRole> UserRoles { get; set; } = [];

    /// <summary>
    /// Role-permission relations.
    /// </summary>
    public List<RolePermission> RolePermissions { get; set; } = [];
}
