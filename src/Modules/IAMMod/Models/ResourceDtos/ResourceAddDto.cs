namespace IAMMod.Models.ResourceDtos;

/// <summary>
/// API resource add DTO
/// </summary>
public class ResourceAddDto
{
    [StringLength(256)]
    public required string Name { get; set; }

    [StringLength(256)]
    public required string DisplayName { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}
