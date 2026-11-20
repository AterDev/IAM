namespace IAMMod.Models.ScopeDtos;

/// <summary>
/// Scope item DTO for list display
/// </summary>
public class ScopeItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool Required { get; set; }
    public DateTimeOffset CreatedTime { get; set; }

    public static ScopeItemDto FromEntity(ApiScope entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        DisplayName = entity.DisplayName,
        Required = entity.Required,
        CreatedTime = entity.CreatedTime,
    };
}
