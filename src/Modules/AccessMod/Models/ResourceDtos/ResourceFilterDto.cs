namespace AccessMod.Models.ResourceDtos;

/// <summary>
/// API resource filter DTO
/// </summary>
public class ResourceFilterDto : FilterBase
{
    /// <summary>
    /// Resource name filter
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Display name filter
    /// </summary>
    public string? DisplayName { get; set; }
}
