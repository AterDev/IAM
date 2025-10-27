namespace CommonMod.Models.SystemSettingDtos;

/// <summary>
/// System setting update DTO
/// </summary>
public class SystemSettingUpdateDto
{
    public required string Value { get; set; }
    public string? Description { get; set; }
}
