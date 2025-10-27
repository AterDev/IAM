namespace CommonMod.Models.SystemSettingDtos;

/// <summary>
/// System setting detail DTO
/// </summary>
public class SystemSettingDetailDto
{
    public Guid Id { get; set; }
    public required string Key { get; set; }
    public required string Value { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public bool IsEditable { get; set; }
    public bool IsPublic { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
}
