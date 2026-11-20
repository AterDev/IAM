namespace Entity.IAMMod;

/// <summary>
/// Role entity for identity management
/// </summary>
public class Role : EntityBase
{
    /// <summary>
    /// Role name
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(256)")]
    public string Name { get; set; }

    /// <summary>
    /// Normalized role name for search
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(256)")]
    public string NormalizedName { get; set; }

    /// <summary>
    /// Concurrency stamp
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(100)")]
    public string? ConcurrencyStamp { get; set; }

    /// <summary>
    /// Role description
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(500)")]
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
