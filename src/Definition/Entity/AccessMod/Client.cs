namespace Entity.AccessMod;

/// <summary>
/// OAuth/OIDC client entity
/// </summary>
[Module(Modules.Access)]
public class Client : EntityBase
{
    /// <summary>
    /// Client identifier
    /// </summary>
    [MaxLength(256)]
    public required string ClientId { get; set; }

    /// <summary>
    /// Client secret hash
    /// </summary>
    [MaxLength(500)]
    public string? ClientSecret { get; set; }

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
    /// Client type (confidential, public, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? Type { get; set; }

    /// <summary>
    /// Require PKCE
    /// </summary>
    public bool RequirePkce { get; set; }

    /// <summary>
    /// Consent type (explicit, implicit, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? ConsentType { get; set; }

    /// <summary>
    /// Application type (web, native, spa)
    /// </summary>
    [MaxLength(50)]
    public string? ApplicationType { get; set; }

    /// <summary>
    /// Permissions (JSON array)
    /// </summary>
    public string? Permissions { get; set; }

    /// <summary>
    /// Requirements (JSON object)
    /// </summary>
    public string? Requirements { get; set; }

    /// <summary>
    /// Settings (JSON object)
    /// </summary>
    public string? Settings { get; set; }

    /// <summary>
    /// Redirect URIs (JSON array)
    /// </summary>
    public List<string> RedirectUris { get; set; } = [];

    /// <summary>
    /// Post logout redirect URIs (JSON array)
    /// </summary>
    public List<string> PostLogoutRedirectUris { get; set; } = [];

    /// <summary>
    /// Client scopes
    /// </summary>
    public List<ClientScope> ClientScopes { get; set; } = [];

    /// <summary>
    /// Authorizations
    /// </summary>
    public List<Authorization> Authorizations { get; set; } = [];
}
