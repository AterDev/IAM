namespace IdentityMod.Models.OAuthDtos;

/// <summary>
/// OAuth/OIDC authorization response DTO
/// </summary>
public class AuthorizeResponseDto
{
    /// <summary>
    /// Authorization code
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Access token (for implicit flow)
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Token type
    /// </summary>
    public string? TokenType { get; set; }

    /// <summary>
    /// Expires in seconds
    /// </summary>
    public int? ExpiresIn { get; set; }

    /// <summary>
    /// ID token (for OIDC)
    /// </summary>
    public string? IdToken { get; set; }

    /// <summary>
    /// State parameter
    /// </summary>
    public string? State { get; set; }

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
