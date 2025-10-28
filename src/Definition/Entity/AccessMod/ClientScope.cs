namespace Entity.AccessMod;

/// <summary>
/// Client scope relationship entity
/// </summary>
[Module(Modules.Access)]
public class ClientScope : EntityBase
{
    /// <summary>
    /// Client ID
    /// </summary>
    public Guid ClientId { get; set; }

    /// <summary>
    /// Scope ID
    /// </summary>
    public Guid ScopeId { get; set; }

    /// <summary>
    /// Client navigation
    /// </summary>
    public Client Client { get; set; } = null!;

    /// <summary>
    /// Scope navigation
    /// </summary>
    public ApiScope Scope { get; set; } = null!;
}
