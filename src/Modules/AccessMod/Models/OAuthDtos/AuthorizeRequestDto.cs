using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace AccessMod.Models.OAuthDtos;

/// <summary>
/// OAuth/OIDC authorization request DTO
/// </summary>
public class AuthorizeRequestDto
{
    /// <summary>
    /// Response type (code, token, id_token)
    /// </summary>
    [ModelBinder(Name = "response_type")]
    [JsonPropertyName("response_type")]
    public required string ResponseType { get; set; }

    /// <summary>
    /// Client identifier
    /// </summary>
    [ModelBinder(Name = "client_id")]
    [JsonPropertyName("client_id")]
    public required string ClientId { get; set; }

    /// <summary>
    /// Redirect URI
    /// </summary>
    [ModelBinder(Name = "redirect_uri")]
    [JsonPropertyName("redirect_uri")]
    public required string RedirectUri { get; set; }

    /// <summary>
    /// Requested scopes (space-separated)
    /// </summary>
    [ModelBinder(Name = "scope")]
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    /// <summary>
    /// State parameter for CSRF protection
    /// </summary>
    [ModelBinder(Name = "state")]
    [JsonPropertyName("state")]
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
    /// Response mode (query, fragment, form_post)
    /// </summary>
    [ModelBinder(Name = "response_mode")]
    [JsonPropertyName("response_mode")]
    public string? ResponseMode { get; set; }

    /// <summary>
    /// Nonce for OIDC
    /// </summary>
    [ModelBinder(Name = "nonce")]
    [JsonPropertyName("nonce")]
    public string? Nonce { get; set; }

    /// <summary>
    /// Prompt parameter (none, login, consent, select_account)
    /// </summary>
    [ModelBinder(Name = "prompt")]
    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }
}
