namespace IdentityMod.Models.RoleDtos;

/// <summary>
/// Role grant permission DTO
/// </summary>
public class RoleGrantPermissionDto
{
    /// <summary>
    /// Permissions to grant to the role (claim type and value pairs)
    /// </summary>
    public required List<PermissionClaim> Permissions { get; set; } = [];
}

/// <summary>
/// Permission claim
/// </summary>
public class PermissionClaim
{
    /// <summary>
    /// Claim type (e.g., "permissions", "resource", etc.)
    /// </summary>
    [MaxLength(256)]
    public required string ClaimType { get; set; }

    /// <summary>
    /// Claim value (e.g., "users.read", "users.write", etc.)
    /// </summary>
    [MaxLength(500)]
    public required string ClaimValue { get; set; }
}
