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
    public required string Name { get; set; }

    /// <summary>
    /// Resource display name
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// Resource description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Creation time
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }
}
