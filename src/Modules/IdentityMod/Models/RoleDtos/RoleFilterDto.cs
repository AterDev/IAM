namespace IdentityMod.Models.RoleDtos;

/// <summary>
/// Role filter DTO
/// </summary>
public class RoleFilterDto : FilterBase
{
    /// <summary>
    /// Filter by role name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Filter by date range start
    /// </summary>
    public DateTimeOffset? StartDate { get; set; }

    /// <summary>
    /// Filter by date range end
    /// </summary>
    public DateTimeOffset? EndDate { get; set; }
}
