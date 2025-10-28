namespace Entity.AccessMod;

/// <summary>
/// Authorization entity for OAuth/OIDC grants
/// </summary>
[Module(Modules.Access)]
public class Authorization : EntityBase
{
    /// <summary>
    /// Subject identifier (user ID)
    /// </summary>
    [MaxLength(256)]
    public required string SubjectId { get; set; }

    /// <summary>
    /// Client ID
    /// </summary>
    public Guid ClientId { get; set; }

    /// <summary>
    /// Authorization type (permanent, ad-hoc)
    /// </summary>
    [MaxLength(50)]
    public string? Type { get; set; }

    /// <summary>
    /// Status (valid, revoked)
    /// </summary>
    [MaxLength(50)]
    public string? Status { get; set; }

    /// <summary>
    /// Scopes (space-separated)
    /// </summary>
    public string? Scopes { get; set; }

    /// <summary>
    /// Properties (JSON object)
    /// </summary>
    public string? Properties { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTimeOffset CreationDate { get; set; }

    /// <summary>
    /// Expiration date
    /// </summary>
    public DateTimeOffset? ExpirationDate { get; set; }

    /// <summary>
    /// Client navigation
    /// </summary>
    public Client Client { get; set; } = null!;

    /// <summary>
    /// Tokens
    /// </summary>
    public List<Token> Tokens { get; set; } = [];
}
