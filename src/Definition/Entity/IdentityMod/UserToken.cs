namespace Entity.IdentityMod;

/// <summary>
/// User token entity for storing authentication tokens
/// </summary>
[Module(Modules.Identity)]
public class UserToken : EntityBase
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Login provider
    /// </summary>
    [MaxLength(128)]
    public required string LoginProvider { get; set; }

    /// <summary>
    /// Token name
    /// </summary>
    [MaxLength(128)]
    public required string Name { get; set; }

    /// <summary>
    /// Token value
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// User navigation
    /// </summary>
    public User User { get; set; } = null!;
}
