namespace AccessMod.Models.ResourceDtos;

/// <summary>
/// API resource add DTO
/// </summary>
public class ResourceAddDto
{
    [MaxLength(256)]
    public required string Name { get; set; }

    [MaxLength(256)]
    public required string DisplayName { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}
