namespace IdentityMod.Models.OrganizationDtos;

/// <summary>
/// Organization add DTO
/// </summary>
public class OrganizationAddDto
{
    [MaxLength(256)]
    public required string Name { get; set; }

    public Guid? ParentId { get; set; }

    public int DisplayOrder { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}
