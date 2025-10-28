namespace AccessMod.Models.ClientDtos;

/// <summary>
/// Client secret rotation response DTO
/// </summary>
public class ClientSecretDto
{
    /// <summary>
    /// New client secret (only returned once)
    /// </summary>
    public required string Secret { get; set; }
}
