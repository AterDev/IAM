namespace IdentityMod.Models.OrganizationDtos;

/// <summary>
/// Organization tree DTO for hierarchical display
/// </summary>
public class OrganizationTreeDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public Guid? ParentId { get; set; }
    public int Level { get; set; }
    public int DisplayOrder { get; set; }
    public string? Description { get; set; }
    public List<OrganizationTreeDto> Children { get; set; } = [];
}
