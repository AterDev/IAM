namespace Entity;

/// <summary>
/// Client redirect URI entity
/// </summary>
[Module(Modules.Access)]
public class ClientRedirectUri : EntityBase
{
    /// <summary>
    /// Client ID
    /// </summary>
    public Guid ClientId { get; set; }

    /// <summary>
    /// Redirect URI
    /// </summary>
    [MaxLength(2000)]
    public required string Uri { get; set; }

    /// <summary>
    /// Client navigation
    /// </summary>
    public Client Client { get; set; } = null!;
}
