namespace Entity.IAMMod;

/// <summary>
/// API resource entity
/// </summary>
public class ApiResource : EntityBase
{
    /// <summary>
    /// Resource name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Properties (JSON object)
    /// </summary>
    public string? Properties { get; set; }

    /// <summary>
    /// Clients that can access this resource
    /// </summary>
    public List<ClientResource> ClientResources { get; set; } = [];
}
