namespace Entity.IAMMod;

/// <summary>
/// Unified permission entity for menus, buttons and business actions.
/// </summary>
public class Permission : EntityBase
{
    /// <summary>
    /// Stable external permission code.
    /// </summary>
    [MaxLength(200)]
    public required string Code { get; set; }

    /// <summary>
    /// Internal permission name.
    /// </summary>
    [MaxLength(200)]
    public required string Name { get; set; }

    /// <summary>
    /// Display name shown in UI.
    /// </summary>
    [MaxLength(200)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Description.
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Permission type.
    /// </summary>
    public PermissionType Type { get; set; }

    /// <summary>
    /// Parent permission id for tree rendering.
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Namespace for structured permission composition.
    /// </summary>
    [MaxLength(100)]
    public string? Namespace { get; set; }

    /// <summary>
    /// Resource for structured permission composition.
    /// </summary>
    [MaxLength(100)]
    public string? Resource { get; set; }

    /// <summary>
    /// Action for structured permission composition.
    /// </summary>
    [MaxLength(100)]
    public string? Action { get; set; }

    /// <summary>
    /// Optional route path when used as a menu.
    /// </summary>
    [MaxLength(500)]
    public string? Path { get; set; }

    /// <summary>
    /// Optional icon when used as a menu/button.
    /// </summary>
    [MaxLength(100)]
    public string? Icon { get; set; }

    /// <summary>
    /// Sort order for menu/button tree.
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// The client that owns this permission tree branch.
    /// </summary>
    public Guid? OwnedClientId { get; set; }

    /// <summary>
    /// Parent navigation.
    /// </summary>
    public Permission? Parent { get; set; }

    /// <summary>
    /// Children permissions.
    /// </summary>
    public List<Permission> Children { get; set; } = [];

    /// <summary>
    /// Client owner navigation.
    /// </summary>
    public Client? OwnedClient { get; set; }

    /// <summary>
    /// Role relations.
    /// </summary>
    public List<RolePermission> RolePermissions { get; set; } = [];

    /// <summary>
    /// Client relations.
    /// </summary>
    public List<ClientPermission> ClientPermissions { get; set; } = [];
}