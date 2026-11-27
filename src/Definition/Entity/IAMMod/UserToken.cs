namespace Entity.IAMMod;

/// <summary>
/// User token entity for storing authentication tokens
/// </summary>
public class UserToken : EntityBase
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Login provider
    /// </summary>
    public string LoginProvider { get; set; }

    /// <summary>
    /// Token name
    /// </summary>
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
