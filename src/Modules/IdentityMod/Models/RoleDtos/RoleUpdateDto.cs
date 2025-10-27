namespace IdentityMod.Models.RoleDtos;

/// <summary>
/// Role update DTO
/// </summary>
public class RoleUpdateDto
{
    [MaxLength(256)]
    public string? Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}
