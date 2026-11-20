namespace IAMMod.Models.OrganizationDtos;

/// <summary>
/// Organization update DTO
/// </summary>
public class OrganizationUpdateDto
{
    [StringLength(256)]
    public string? Name { get; set; }

    public Guid? ParentId { get; set; }

    public int? DisplayOrder { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}
