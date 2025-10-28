namespace AccessMod.Models.ResourceDtos;

/// <summary>
/// API resource item DTO for list views
/// </summary>
public class ResourceItemDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
}
