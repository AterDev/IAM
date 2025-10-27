namespace CommonMod.Models.SystemSettingDtos;

/// <summary>
/// System setting item DTO for list views
/// </summary>
public class SystemSettingItemDto
{
    public Guid Id { get; set; }
    public required string Key { get; set; }
    public required string Value { get; set; }
    public string? Category { get; set; }
    public bool IsEditable { get; set; }
    public bool IsPublic { get; set; }
}
