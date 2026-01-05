using Microsoft.AspNetCore.Mvc;

namespace IdentityMod.Models.OAuthDtos;

/// <summary>
/// Device authorization request DTO
/// </summary>
public class DeviceAuthorizationRequestDto
{
    /// <summary>
    /// Client identifier
    /// </summary>
    [ModelBinder(Name = "client_id")]
    public required string ClientId { get; set; }

    /// <summary>
    /// Requested scope
    /// </summary>
    [ModelBinder(Name = "scope")]
    public string? Scope { get; set; }
}
