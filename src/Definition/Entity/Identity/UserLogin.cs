namespace Entity.Identity;

/// <summary>
/// User login entity for external authentication
/// </summary>
[Module(Modules.Identity)]
public class UserLogin : EntityBase
{
    /// <summary>
    /// Login provider name
    /// </summary>
    [MaxLength(128)]
    public required string LoginProvider { get; set; }

    /// <summary>
    /// Provider key
    /// </summary>
    [MaxLength(128)]
    public required string ProviderKey { get; set; }

    /// <summary>
    /// Provider display name
    /// </summary>
    [MaxLength(256)]
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
