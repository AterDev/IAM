namespace IAMMod.Models.OAuthDtos;

/// <summary>
/// Scope metadata for OAuth interaction pages.
/// </summary>
public class OAuthInteractionScopeDto
{
    /// <summary>
    /// Scope name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// User-facing scope display name.
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// Optional scope description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the scope is required.
    /// </summary>
    public bool Required { get; set; }
}