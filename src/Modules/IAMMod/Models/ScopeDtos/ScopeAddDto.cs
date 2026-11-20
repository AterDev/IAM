namespace IAMMod.Models.ScopeDtos;

/// <summary>
/// Scope add DTO
/// </summary>
public class ScopeAddDto
{
    [StringLength(256)]
    public required string Name { get; set; }

    [StringLength(256)]
    public required string DisplayName { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public bool Required { get; set; }

    public bool Emphasize { get; set; }

    public List<string> Claims { get; set; } = [];
}
