using System.Text.Json.Serialization;

namespace IdentityMod.Models.OAuthDtos;

/// <summary>
/// Device authorization request DTO
/// </summary>
public class DeviceAuthorizationRequestDto
{
    /// <summary>
    /// Client identifier
    /// </summary>
    [JsonPropertyName("client_id")]
    public required string ClientId { get; set; }

    /// <summary>
    /// Requested scope
    /// </summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}
