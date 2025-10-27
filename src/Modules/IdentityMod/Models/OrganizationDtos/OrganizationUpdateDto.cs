namespace IdentityMod.Models.OrganizationDtos;

/// <summary>
/// Organization update DTO
/// </summary>
public class OrganizationUpdateDto
{
    [MaxLength(256)]
    public string? Name { get; set; }

    public Guid? ParentId { get; set; }

    public int? DisplayOrder { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}
