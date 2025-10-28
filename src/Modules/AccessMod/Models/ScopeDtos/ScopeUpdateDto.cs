namespace AccessMod.Models.ScopeDtos;

/// <summary>
/// Scope update DTO
/// </summary>
public class ScopeUpdateDto
{
    [MaxLength(256)]
    public string? DisplayName { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool? Required { get; set; }

    public bool? Emphasize { get; set; }

    public List<string>? Claims { get; set; }
}
