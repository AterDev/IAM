namespace IAMMod.Models.ResourceDtos;

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

    public static ResourceItemDto FromEntity(ApiResource entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        DisplayName = entity.DisplayName,
        Description = entity.Description,
        CreatedTime = entity.CreatedTime,
    };
}
