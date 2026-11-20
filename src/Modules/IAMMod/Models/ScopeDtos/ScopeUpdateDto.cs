namespace IAMMod.Models.ScopeDtos;

/// <summary>
/// Scope update DTO
/// </summary>
public class ScopeUpdateDto
{
    [StringLength(256)]
    public string? DisplayName { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public bool? Required { get; set; }

    public bool? Emphasize { get; set; }

    public List<string>? Claims { get; set; }
}
