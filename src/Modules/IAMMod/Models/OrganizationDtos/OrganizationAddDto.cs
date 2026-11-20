namespace IAMMod.Models.OrganizationDtos;

/// <summary>
/// Organization add DTO
/// </summary>
public class OrganizationAddDto
{
    [StringLength(256)]
    public required string Name { get; set; }

    public Guid? ParentId { get; set; }

    public int DisplayOrder { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}
