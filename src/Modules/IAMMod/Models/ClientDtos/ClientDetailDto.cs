using Entity.IAMMod;
using IAMMod.Models.ResourceDtos;
using IAMMod.Models.ScopeDtos;

namespace IAMMod.Models.ClientDtos;

/// <summary>
/// Client detail DTO
/// </summary>
public class ClientDetailDto
{
    public Guid Id { get; set; }
    public required string ClientId { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public ClientType? Type { get; set; }
    public bool RequirePkce { get; set; }
    public ConsentType? ConsentType { get; set; }
    public ApplicationType? ApplicationType { get; set; }
    public ClientRegistrationStatus RegistrationStatus { get; set; }
    public Guid? DeveloperUserId { get; set; }
    public DateTimeOffset? RequestedTime { get; set; }
    public DateTimeOffset? ReviewedTime { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTimeOffset? SecretExpiresAt { get; set; }
    public bool AllowPasswordGrant { get; set; }
    public string? PasswordGrantRestrictionReason { get; set; }
    public List<string> RedirectUris { get; set; } = [];
    public List<string> PostLogoutRedirectUris { get; set; } = [];
    public List<ScopeItemDto> Scopes { get; set; } = [];
    /// <summary>
    /// API resources this client can access
    /// </summary>
    public List<ClientResourceDto> Resources { get; set; } = [];
    public List<ClientSecretHistoryDto> Secrets { get; set; } = [];
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
}
