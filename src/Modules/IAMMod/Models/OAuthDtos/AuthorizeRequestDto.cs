using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace IAMMod.Models.OAuthDtos;

/// <summary>
/// OAuth/OIDC authorization request DTO
/// </summary>
public class AuthorizeRequestDto
{
    /// <summary>
    /// Response type. Currently only authorization code flow is supported.
    /// </summary>
    [ModelBinder(Name = OpenIdConnectParameterNames.ResponseType)]
    [JsonPropertyName(OpenIdConnectParameterNames.ResponseType)]
    public required string ResponseType { get; set; }

    /// <summary>
    /// Client identifier
    /// </summary>
    [ModelBinder(Name = OpenIdConnectParameterNames.ClientId)]
    [JsonPropertyName(OpenIdConnectParameterNames.ClientId)]
    public required string ClientId { get; set; }

    /// <summary>
    /// Redirect URI
    /// </summary>
    [ModelBinder(Name = OpenIdConnectParameterNames.RedirectUri)]
    [JsonPropertyName(OpenIdConnectParameterNames.RedirectUri)]
    public required string RedirectUri { get; set; }

    /// <summary>
    /// Requested scopes (space-separated)
    /// </summary>
    [ModelBinder(Name = OpenIdConnectParameterNames.Scope)]
    [JsonPropertyName(OpenIdConnectParameterNames.Scope)]
    public string? Scope { get; set; }

    /// <summary>
    /// State parameter for CSRF protection
    /// </summary>
    [ModelBinder(Name = OpenIdConnectParameterNames.State)]
    [JsonPropertyName(OpenIdConnectParameterNames.State)]
    public string? State { get; set; }

    /// <summary>
    /// PKCE code challenge
    /// </summary>
    [ModelBinder(Name = "code_challenge")]
    [JsonPropertyName("code_challenge")]
    public string? CodeChallenge { get; set; }

    /// <summary>
    /// PKCE code challenge method (plain, S256)
    /// </summary>
    [ModelBinder(Name = "code_challenge_method")]
    [JsonPropertyName("code_challenge_method")]
    public string? CodeChallengeMethod { get; set; }

    /// <summary>
    /// Response mode. Currently only query mode is supported.
    /// </summary>
    [ModelBinder(Name = "response_mode")]
    [JsonPropertyName("response_mode")]
    public string? ResponseMode { get; set; }

    /// <summary>
    /// Nonce for OIDC
    /// </summary>
    [ModelBinder(Name = OpenIdConnectParameterNames.Nonce)]
    [JsonPropertyName(OpenIdConnectParameterNames.Nonce)]
    public string? Nonce { get; set; }

    /// <summary>
    /// Prompt parameter (none, login, consent, select_account)
    /// </summary>
    [ModelBinder(Name = OpenIdConnectParameterNames.Prompt)]
    [JsonPropertyName(OpenIdConnectParameterNames.Prompt)]
    public string? Prompt { get; set; }
}
