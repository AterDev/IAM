namespace IAMMod.Models.ResourceDtos;

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

    public static ResourceDetailDto FromEntity(ApiResource entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        DisplayName = entity.DisplayName,
        Description = entity.Description,
        CreatedTime = entity.CreatedTime,
        UpdatedTime = entity.UpdatedTime,
    };
}
