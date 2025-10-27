namespace AccessMod.Models.ScopeDtos;

/// <summary>
/// Scope item DTO for list display
/// </summary>
public class ScopeItemDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public bool Required { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
}
