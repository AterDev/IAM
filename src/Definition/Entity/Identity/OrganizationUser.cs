namespace Entity.Identity;

/// <summary>
/// Organization user relationship entity
/// </summary>
[Module(Modules.Identity)]
public class OrganizationUser : EntityBase
{
    /// <summary>
    /// Organization ID
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Is primary organization
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Organization navigation
    /// </summary>
    public Organization Organization { get; set; } = null!;

    /// <summary>
    /// User navigation
    /// </summary>
    public User User { get; set; } = null!;
}
