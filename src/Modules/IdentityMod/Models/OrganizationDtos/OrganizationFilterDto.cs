namespace IdentityMod.Models.OrganizationDtos;

/// <summary>
/// Organization filter DTO
/// </summary>
public class OrganizationFilterDto : FilterBase
{
    /// <summary>
    /// Filter by organization name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Filter by parent ID
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Filter by level
    /// </summary>
    public int? Level { get; set; }
}
