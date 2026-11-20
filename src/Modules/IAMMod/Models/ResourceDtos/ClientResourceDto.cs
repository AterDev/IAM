namespace IAMMod.Models.ResourceDtos;

/// <summary>
/// Client resource DTO
/// </summary>
public class ClientResourceDto
{
    /// <summary>
    /// Resource ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Resource name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Resource display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Resource description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Creation time
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }
}
