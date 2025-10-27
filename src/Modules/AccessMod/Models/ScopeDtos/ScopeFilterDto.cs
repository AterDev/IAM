namespace AccessMod.Models.ScopeDtos;

/// <summary>
/// Scope filter DTO
/// </summary>
public class ScopeFilterDto : FilterBase
{
    /// <summary>
    /// Filter by scope name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Filter by display name
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Filter by required flag
    /// </summary>
    public bool? Required { get; set; }
}
