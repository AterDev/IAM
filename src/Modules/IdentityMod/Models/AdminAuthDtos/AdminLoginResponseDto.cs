namespace IdentityMod.Models.AdminAuthDtos;

/// <summary>
/// Admin login response DTO
/// </summary>
public class AdminLoginResponseDto
{
    /// <summary>
    /// JWT access token
    /// </summary>
    public required string AccessToken { get; set; }

    /// <summary>
    /// Token type (Bearer)
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Token expiration in seconds
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// User information
    /// </summary>
    public required AdminUserInfo User { get; set; }
}

/// <summary>
/// Admin user information
/// </summary>
public class AdminUserInfo
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Username
    /// </summary>
    public required string UserName { get; set; }

    /// <summary>
    /// Email address
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// User roles
    /// </summary>
    public List<string> Roles { get; set; } = [];
}
