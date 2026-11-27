namespace Entity.IAMMod;

/// <summary>
/// Role-permission relation.
/// </summary>
public class RolePermission : EntityBase
{
    /// <summary>
    /// Role id.
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Permission id.
    /// </summary>
    public Guid PermissionId { get; set; }

    /// <summary>
    /// Role navigation.
    /// </summary>
    public Role Role { get; set; } = null!;

    /// <summary>
    /// Permission navigation.
    /// </summary>
    public Permission Permission { get; set; } = null!;
}