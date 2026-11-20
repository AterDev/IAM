namespace Entity.IAMMod;

/// <summary>
/// User claim entity
/// </summary>
public class UserClaim : EntityBase
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Claim type
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(256)")]
    public string ClaimType { get; set; }

    /// <summary>
    /// Claim value
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(500)")]
    public string? ClaimValue { get; set; }

    /// <summary>
    /// User navigation
    /// </summary>
    public User User { get; set; } = null!;
}
