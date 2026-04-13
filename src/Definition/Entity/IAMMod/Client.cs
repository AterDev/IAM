namespace Entity.IAMMod;

/// <summary>
/// OAuth/OIDC client entity
/// </summary>
public class Client : EntityBase
{
    /// <summary>
    /// Client identifier
    /// </summary>
    [MaxLength(100)]
    public required string ClientId { get; set; }

    /// <summary>
    /// Client secret hash
    /// </summary>
    [MaxLength(200)]
    public string? SecretHash { get; set; }

    [MaxLength(100)]
    public string? SecretSalt { get; set; }

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
    public ClientType? Type { get; set; }

    /// <summary>
    /// Require PKCE
    /// </summary>
    public bool RequirePkce { get; set; }

    /// <summary>
    /// Consent type (explicit, implicit, etc.)
    /// </summary>
    public ConsentType? ConsentType { get; set; }

    /// <summary>
    /// Application type (web, native, spa)
    /// </summary>
    public ApplicationType? ApplicationType { get; set; }

    /// <summary>
    /// Lifecycle status for self-service and approval scenarios.
    /// </summary>
    public ClientRegistrationStatus RegistrationStatus { get; set; } = ClientRegistrationStatus.Approved;

    /// <summary>
    /// Owner user id for self-service registrations.
    /// </summary>
    public Guid? DeveloperUserId { get; set; }

    /// <summary>
    /// Time when the registration was requested.
    /// </summary>
    public DateTimeOffset? RequestedTime { get; set; }

    /// <summary>
    /// Time when the client was reviewed.
    /// </summary>
    public DateTimeOffset? ReviewedTime { get; set; }

    /// <summary>
    /// Reviewer identifier.
    /// </summary>
    [MaxLength(100)]
    public string? ReviewedBy { get; set; }

    /// <summary>
    /// Expiration time for the current active secret.
    /// </summary>
    public DateTimeOffset? SecretExpiresAt { get; set; }

    /// <summary>
    /// Whether the password grant is still allowed for this client.
    /// </summary>
    public bool AllowPasswordGrant { get; set; } = false;

    /// <summary>
    /// Optional reason or migration hint when password grant is disabled.
    /// </summary>
    [MaxLength(500)]
    public string? PasswordGrantRestrictionReason { get; set; }


    /// <summary>
    /// Redirect URIs
    /// </summary>
    public List<string> RedirectUris { get; set; } = [];

    /// <summary>
    /// Post logout redirect URIs 
    /// </summary>
    public List<string> PostLogoutRedirectUris { get; set; } = [];

    /// <summary>
    /// Client scopes
    /// </summary>
    public List<ClientScope> ClientScopes { get; set; } = [];

    /// <summary>
    /// Client resources (API resources this client can access)
    /// </summary>
    public List<ClientResource> ClientResources { get; set; } = [];

    /// <summary>
    /// Authorizations
    /// </summary>
    public List<Authorization> Authorizations { get; set; } = [];

    /// <summary>
    /// Historical secrets issued for this client.
    /// </summary>
    public List<ClientSecret> ClientSecrets { get; set; } = [];

    /// <summary>
    /// Permissions assigned to the client.
    /// </summary>
    public List<ClientPermission> ClientPermissions { get; set; } = [];

    /// <summary>
    /// Permissions owned by the client.
    /// </summary>
    public List<Permission> OwnedPermissions { get; set; } = [];
}
