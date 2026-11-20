namespace Entity.IAMMod;

/// <summary>
/// User token entity for storing authentication tokens
/// </summary>
[Perigon.PostgreSQL.Attributes.Index(nameof(UserToken.UserId), nameof(UserToken.LoginProvider), nameof(UserToken.Name), IsUnique = true)]
public class UserToken : EntityBase
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Login provider
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(128)")]
    public string LoginProvider { get; set; }

    /// <summary>
    /// Token name
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(128)")]
    public string Name { get; set; }

    /// <summary>
    /// Token value
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// User navigation
    /// </summary>
    public User User { get; set; } = null!;
}
