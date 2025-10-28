namespace CommonMod.Models.SystemSettingDtos;

/// <summary>
/// System setting add DTO
/// </summary>
public class SystemSettingAddDto
{
    public required string Key { get; set; }
    public required string Value { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public bool IsEditable { get; set; } = true;
    public bool IsPublic { get; set; } = false;
}
