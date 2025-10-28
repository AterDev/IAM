namespace AccessMod.Models.ResourceDtos;

/// <summary>
/// API resource update DTO
/// </summary>
public class ResourceUpdateDto
{
    [MaxLength(256)]
    public string? DisplayName { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}
