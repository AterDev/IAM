namespace IdentityMod.Models.OrganizationDtos;

/// <summary>
/// Organization detail DTO
/// </summary>
public class OrganizationDetailDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public Guid? ParentId { get; set; }
    public string? Path { get; set; }
    public int Level { get; set; }
    public int DisplayOrder { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
}
