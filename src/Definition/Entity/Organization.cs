namespace Entity;

/// <summary>
/// Organization entity for hierarchical structure
/// </summary>
[Module(Modules.Identity)]
public class Organization : EntityBase
{
    /// <summary>
    /// Organization name
    /// </summary>
    [MaxLength(256)]
    public required string Name { get; set; }

    /// <summary>
    /// Parent organization ID
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Hierarchical path (e.g., /1/2/3/)
    /// </summary>
    [MaxLength(1000)]
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
    [MaxLength(500)]
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
