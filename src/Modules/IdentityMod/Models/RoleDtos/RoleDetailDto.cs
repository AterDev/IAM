namespace IdentityMod.Models.RoleDtos;

/// <summary>
/// Role detail DTO
/// </summary>
public class RoleDetailDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
}
