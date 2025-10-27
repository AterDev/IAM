namespace Entity;

/// <summary>
/// API resource entity
/// </summary>
[Module(Modules.Access)]
public class ApiResource : EntityBase
{
    /// <summary>
    /// Resource name
    /// </summary>
    [MaxLength(256)]
    public required string Name { get; set; }

    /// <summary>
    /// Display name
    /// </summary>
    [MaxLength(256)]
    public required string DisplayName { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Properties (JSON object)
    /// </summary>
    public string? Properties { get; set; }
}
