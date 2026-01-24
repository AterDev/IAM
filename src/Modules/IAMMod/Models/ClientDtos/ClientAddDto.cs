using Entity.IAMMod;

namespace IAMMod.Models.ClientDtos;

/// <summary>
/// Client add DTO
/// </summary>
public class ClientAddDto
{
    [MaxLength(256)]
    public required string ClientId { get; set; }

    [MaxLength(256)]
    public required string DisplayName { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public ClientType? Type { get; set; }

    public bool RequirePkce { get; set; } = true;

    public ConsentType? ConsentType { get; set; }

    public ApplicationType? ApplicationType { get; set; }

    public List<string> RedirectUris { get; set; } = [];
    public List<string> PostLogoutRedirectUris { get; set; } = [];
    public List<Guid> ScopeIds { get; set; } = [];
    /// <summary>
    /// API resource IDs this client can access
    /// </summary>
    public List<Guid> ResourceIds { get; set; } = [];
}
