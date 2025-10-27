namespace Entity;

/// <summary>
/// API scope entity
/// </summary>
[Module(Modules.Access)]
public class ApiScope : EntityBase
{
    /// <summary>
    /// Scope name
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
