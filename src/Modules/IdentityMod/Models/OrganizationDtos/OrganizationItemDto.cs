namespace IdentityMod.Models.OrganizationDtos;

/// <summary>
/// Organization item DTO for list display
/// </summary>
public class OrganizationItemDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public Guid? ParentId { get; set; }
    public int Level { get; set; }
    public int DisplayOrder { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
}
