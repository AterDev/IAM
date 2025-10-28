namespace AccessMod.Models.ClientDtos;

/// <summary>
/// Client filter DTO
/// </summary>
public class ClientFilterDto : FilterBase
{
    /// <summary>
    /// Filter by client ID
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Filter by display name
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Filter by client type
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Filter by application type
    /// </summary>
    public string? ApplicationType { get; set; }
}
