namespace IAMMod.Models.RoleDtos;

/// <summary>
/// Role update DTO
/// </summary>
public class RoleUpdateDto
{
    [StringLength(256)]
    public string? Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}
