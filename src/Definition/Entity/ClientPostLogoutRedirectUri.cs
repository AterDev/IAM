namespace Entity;

/// <summary>
/// Client post logout redirect URI entity
/// </summary>
[Module(Modules.Access)]
public class ClientPostLogoutRedirectUri : EntityBase
{
    /// <summary>
    /// Client ID
    /// </summary>
    public Guid ClientId { get; set; }

    /// <summary>
    /// Post logout redirect URI
    /// </summary>
    [MaxLength(2000)]
    public required string Uri { get; set; }

    /// <summary>
    /// Client navigation
    /// </summary>
    public Client Client { get; set; } = null!;
}
