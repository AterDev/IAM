namespace IdentityMod.Models.OAuthDtos;

/// <summary>
/// Token introspection response DTO
/// </summary>
public class IntrospectResponseDto
{
    /// <summary>
    /// Whether the token is active
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    /// Scope
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// Client ID
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Username
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Token type
    /// </summary>
    public string? TokenType { get; set; }

    /// <summary>
    /// Expiration time (Unix timestamp)
    /// </summary>
    public long? Exp { get; set; }

    /// <summary>
    /// Issued at time (Unix timestamp)
    /// </summary>
    public long? Iat { get; set; }

    /// <summary>
    /// Not before time (Unix timestamp)
    /// </summary>
    public long? Nbf { get; set; }

    /// <summary>
    /// Subject
    /// </summary>
    public string? Sub { get; set; }

    /// <summary>
    /// Audience
    /// </summary>
    public string? Aud { get; set; }

    /// <summary>
    /// Issuer
    /// </summary>
    public string? Iss { get; set; }

    /// <summary>
    /// JWT ID
    /// </summary>
    public string? Jti { get; set; }
}
