namespace Entity.IAMMod;

/// <summary>
/// User login entity for external authentication
/// </summary>
[Perigon.PostgreSQL.Attributes.Index(nameof(UserLogin.LoginProvider), nameof(UserLogin.ProviderKey), IsUnique = true)]
public class UserLogin : EntityBase
{
    /// <summary>
    /// Login provider name
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(128)")]
    public string LoginProvider { get; set; }

    /// <summary>
    /// Provider key
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(128)")]
    public string ProviderKey { get; set; }

    /// <summary>
    /// Provider display name
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(256)")]
    public string? ProviderDisplayName { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User navigation
    /// </summary>
    public User User { get; set; } = null!;
}
