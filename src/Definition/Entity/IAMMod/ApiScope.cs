namespace Entity.IAMMod;

/// <summary>
/// API scope entity
/// </summary>
public class ApiScope : EntityBase
{
    /// <summary>
    /// Scope name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Display name
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Resources (JSON array)
    /// </summary>
    public string? Resources { get; set; }

    /// <summary>
    /// Properties (JSON object)
    /// </summary>
    public string? Properties { get; set; }

    /// <summary>
    /// Whether this scope is required
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Whether to emphasize this scope in consent UI
    /// </summary>
    public bool Emphasize { get; set; }

    /// <summary>
    /// Scope claims
    /// </summary>
    public List<ScopeClaim> ScopeClaims { get; set; } = [];

    /// <summary>
    /// Client scopes
    /// </summary>
    public List<ClientScope> ClientScopes { get; set; } = [];
}
