namespace Entity.CommonMod;

/// <summary>
/// System setting entity for application configuration
/// </summary>
[Module(Modules.Common)]
public class SystemSetting : EntityBase
{
    /// <summary>
    /// Setting key (unique identifier)
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// Setting value
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// Setting description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Setting category for grouping
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Whether the setting is editable
    /// </summary>
    public bool IsEditable { get; set; } = true;

    /// <summary>
    /// Whether the setting is public (accessible without authentication)
    /// </summary>
    public bool IsPublic { get; set; } = false;
}
