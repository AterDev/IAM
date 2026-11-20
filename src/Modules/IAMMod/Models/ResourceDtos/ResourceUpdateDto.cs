namespace IAMMod.Models.ResourceDtos;

/// <summary>
/// API resource update DTO
/// </summary>
public class ResourceUpdateDto
{
    [StringLength(256)]
    public string? DisplayName { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}
