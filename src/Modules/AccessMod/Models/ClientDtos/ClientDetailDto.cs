namespace AccessMod.Models.ClientDtos;

/// <summary>
/// Client detail DTO
/// </summary>
public class ClientDetailDto
{
    public Guid Id { get; set; }
    public required string ClientId { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public bool RequirePkce { get; set; }
    public string? ConsentType { get; set; }
    public string? ApplicationType { get; set; }
    public List<string> RedirectUris { get; set; } = [];
    public List<string> PostLogoutRedirectUris { get; set; } = [];
    public List<string> Scopes { get; set; } = [];
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
}
