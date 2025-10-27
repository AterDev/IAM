namespace AccessMod.Models.ClientDtos;

/// <summary>
/// Client update DTO
/// </summary>
public class ClientUpdateDto
{
    [MaxLength(256)]
    public string? DisplayName { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? Type { get; set; }

    public bool? RequirePkce { get; set; }

    [MaxLength(50)]
    public string? ConsentType { get; set; }

    [MaxLength(50)]
    public string? ApplicationType { get; set; }

    public List<string>? RedirectUris { get; set; }
    public List<string>? PostLogoutRedirectUris { get; set; }
}
