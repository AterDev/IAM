namespace Entity.IAMMod;

/// <summary>
/// Unified permission entity for menus, buttons and business actions.
/// </summary>
public class Permission : EntityBase
{
    /// <summary>
    /// Stable external permission code.
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(200)")]
    public string Code { get; set; }

    /// <summary>
    /// Internal permission name.
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(200)")]
    public string Name { get; set; }

    /// <summary>
    /// Description.
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(1000)")]
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
    /// Optional route path when used as a menu.
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(500)")]
    public string? Path { get; set; }

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
