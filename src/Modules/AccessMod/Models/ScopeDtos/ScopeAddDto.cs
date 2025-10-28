namespace AccessMod.Models.ScopeDtos;

/// <summary>
/// Scope add DTO
/// </summary>
public class ScopeAddDto
{
    [MaxLength(256)]
    public required string Name { get; set; }

    [MaxLength(256)]
    public required string DisplayName { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool Required { get; set; }

    public bool Emphasize { get; set; }

    public List<string> Claims { get; set; } = [];
}
