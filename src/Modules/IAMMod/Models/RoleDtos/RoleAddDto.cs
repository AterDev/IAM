namespace IAMMod.Models.RoleDtos;

/// <summary>
/// Role add DTO
/// </summary>
public class RoleAddDto
{
    [StringLength(256)]
    public required string Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}
