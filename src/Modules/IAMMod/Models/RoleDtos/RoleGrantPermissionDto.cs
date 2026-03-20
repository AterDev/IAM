namespace IAMMod.Models.RoleDtos;

/// <summary>
/// Role grant permission DTO
/// </summary>
public class RoleGrantPermissionDto
{
    /// <summary>
    /// Permission codes to grant to the role.
    /// </summary>
    public List<string> PermissionCodes { get; set; } = [];
}
