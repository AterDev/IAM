namespace IdentityMod.Models.RoleDtos;

/// <summary>
/// Role item DTO for list display
/// </summary>
public class RoleItemDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
}
