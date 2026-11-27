namespace Entity.IAMMod;

using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// OAuth/OIDC client entity
/// </summary>
public class Client : EntityBase
{
    /// <summary>
    /// Client identifier
    /// </summary>
    [Column("ClientId")]
    public string ClientIdentifier { get; set; } = string.Empty;

    /// <summary>OAuth client identifier exposed by the domain API.</summary>
    [NotMapped]
    public string ClientId
    {
        get => ClientIdentifier;
        set => ClientIdentifier = value;
    }

    /// <summary>
    /// Client secret hash
    /// </summary>
    public string? SecretHash { get; set; }

    public string? SecretSalt { get; set; }

    /// <summary>
    /// Display name
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Description
    /// </summary>
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
    public string? ReviewedBy { get; set; }

    /// <summary>
    /// Expiration time for the current active secret.
    /// </summary>
    public DateTimeOffset? SecretExpiresAt { get; set; }

    /// <summary>
    /// Redirect URIs
    /// </summary>
    public string[] RedirectUriValues { get; set; } = [];

    /// <summary>Redirect URIs exposed as a mutable domain collection.</summary>
    [NotMapped]
    public List<string> RedirectUris
    {
        get => [.. RedirectUriValues];
        set => RedirectUriValues = [.. value];
    }

    /// <summary>
    /// Post logout redirect URIs 
    /// </summary>
    public string[] PostLogoutRedirectUriValues { get; set; } = [];

    /// <summary>Post-logout redirect URIs exposed as a mutable domain collection.</summary>
    [NotMapped]
    public List<string> PostLogoutRedirectUris
    {
        get => [.. PostLogoutRedirectUriValues];
        set => PostLogoutRedirectUriValues = [.. value];
    }

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
