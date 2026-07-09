namespace Entity.UserCenterMod;

/// <summary>
/// Daily proxy traffic usage for a user.
/// </summary>
public class ProxyUsage : EntityBase
{
    /// <summary>
    /// User identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Usage date in server local date.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Traffic usage in bytes.
    /// </summary>
    public long Usage { get; set; }
}
