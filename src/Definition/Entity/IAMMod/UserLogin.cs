namespace Entity.IAMMod;

/// <summary>
/// User login entity for external authentication
/// </summary>
public class UserLogin : EntityBase
{
    /// <summary>
    /// Login provider name
    /// </summary>
    public string LoginProvider { get; set; }

    /// <summary>
    /// Provider key
    /// </summary>
    public string ProviderKey { get; set; }

    /// <summary>
    /// Provider display name
    /// </summary>
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
