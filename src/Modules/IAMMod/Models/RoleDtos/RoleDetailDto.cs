namespace IAMMod.Models.RoleDtos;

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

    public static RoleDetailDto FromEntity(Role entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        CreatedTime = entity.CreatedTime,
        UpdatedTime = entity.UpdatedTime,
    };
}
