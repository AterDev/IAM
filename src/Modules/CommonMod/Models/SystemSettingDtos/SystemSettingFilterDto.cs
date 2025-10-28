namespace CommonMod.Models.SystemSettingDtos;

/// <summary>
/// System setting filter DTO
/// </summary>
public class SystemSettingFilterDto : FilterBase
{
    /// <summary>
    /// Filter by key (partial match)
    /// </summary>
    public string? Key { get; set; }
    
    /// <summary>
    /// Filter by category
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// Filter by public flag
    /// </summary>
    public bool? IsPublic { get; set; }
    
    /// <summary>
    /// Filter by editable flag
    /// </summary>
    public bool? IsEditable { get; set; }
}
