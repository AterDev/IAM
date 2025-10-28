namespace IdentityMod.Models.RoleDtos;

/// <summary>
/// Role add DTO
/// </summary>
public class RoleAddDto
{
    [MaxLength(256)]
    public required string Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}
