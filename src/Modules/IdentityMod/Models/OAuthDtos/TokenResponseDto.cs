namespace IdentityMod.Models.OAuthDtos;

/// <summary>
/// OAuth/OIDC token response DTO
/// </summary>
public class TokenResponseDto
{
    /// <summary>
    /// Access token
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Token type (usually "Bearer")
    /// </summary>
    public string? TokenType { get; set; }

    /// <summary>
    /// Expires in seconds
    /// </summary>
    public int? ExpiresIn { get; set; }

    /// <summary>
    /// Refresh token
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// ID token (OIDC)
    /// </summary>
    public string? IdToken { get; set; }

    /// <summary>
    /// Scope granted
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// Error code
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Error description
    /// </summary>
    public string? ErrorDescription { get; set; }
}
