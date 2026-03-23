using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace IAMMod.Models.OAuthDtos;

/// <summary>
/// Device authorization request DTO
/// </summary>
public class DeviceAuthorizationRequestDto
{
    /// <summary>
    /// Client identifier
    /// </summary>
    [ModelBinder(Name = OpenIdConnectParameterNames.ClientId)]
    public required string ClientId { get; set; }

    /// <summary>
    /// Requested scope
    /// </summary>
    [ModelBinder(Name = OpenIdConnectParameterNames.Scope)]
    public string? Scope { get; set; }
}
