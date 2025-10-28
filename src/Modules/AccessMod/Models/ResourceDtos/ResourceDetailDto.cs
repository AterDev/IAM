namespace AccessMod.Models.ResourceDtos;

/// <summary>
/// API resource detail DTO
/// </summary>
public class ResourceDetailDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
}
