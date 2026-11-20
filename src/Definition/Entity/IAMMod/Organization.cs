namespace Entity.IAMMod;

/// <summary>
/// Organization entity for hierarchical structure
/// </summary>
public class Organization : EntityBase
{
    /// <summary>
    /// Organization name
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(256)")]
    public string Name { get; set; }

    /// <summary>
    /// Parent organization ID
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Hierarchical path (e.g., /1/2/3/)
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(1000)")]
    public string? Path { get; set; }

    /// <summary>
    /// Level in hierarchy
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(500)")]
    public string? Description { get; set; }

    /// <summary>
    /// Parent organization navigation
    /// </summary>
    public Organization? Parent { get; set; }

    /// <summary>
    /// Child organizations
    /// </summary>
    public List<Organization> Children { get; set; } = [];

    /// <summary>
    /// Organization users
    /// </summary>
    public List<OrganizationUser> OrganizationUsers { get; set; } = [];
}
