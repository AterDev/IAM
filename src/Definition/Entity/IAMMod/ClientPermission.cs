namespace Entity.IAMMod;

/// <summary>
/// Client-permission relation.
/// </summary>
[Perigon.PostgreSQL.Attributes.Index(nameof(ClientPermission.ClientId), nameof(ClientPermission.PermissionId), IsUnique = true)]
public class ClientPermission : EntityBase
{
    /// <summary>
    /// Client id.
    /// </summary>
    public Guid ClientId { get; set; }

    /// <summary>
    /// Permission id.
    /// </summary>
    public Guid PermissionId { get; set; }

    /// <summary>
    /// Client navigation.
    /// </summary>
    public Client Client { get; set; } = null!;

    /// <summary>
    /// Permission navigation.
    /// </summary>
    public Permission Permission { get; set; } = null!;
}