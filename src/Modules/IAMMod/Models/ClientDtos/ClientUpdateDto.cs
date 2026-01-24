using Entity.IAMMod;

namespace IAMMod.Models.ClientDtos;

/// <summary>
/// Client update DTO
/// </summary>
public class ClientUpdateDto
{
    [MaxLength(256)]
    public string? DisplayName { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public ClientType? Type { get; set; }

    public bool? RequirePkce { get; set; }

    public ConsentType? ConsentType { get; set; }

    public ApplicationType? ApplicationType { get; set; }

    public List<string>? RedirectUris { get; set; }
    public List<string>? PostLogoutRedirectUris { get; set; }
    public List<Guid>? ScopeIds { get; set; }
    /// <summary>
    /// API resource IDs this client can access
    /// </summary>
    public List<Guid>? ResourceIds { get; set; }
}
