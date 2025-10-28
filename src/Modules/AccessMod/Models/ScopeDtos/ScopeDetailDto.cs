namespace AccessMod.Models.ScopeDtos;

/// <summary>
/// Scope detail DTO
/// </summary>
public class ScopeDetailDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public bool Required { get; set; }
    public bool Emphasize { get; set; }
    public List<string> Claims { get; set; } = [];
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
}
