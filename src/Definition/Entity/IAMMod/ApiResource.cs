namespace Entity.IAMMod;

/// <summary>
/// API resource entity
/// </summary>
public class ApiResource : EntityBase
{
    /// <summary>
    /// Resource name
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(256)")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(256)")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(500)")]
    public string? Description { get; set; }

    /// <summary>
    /// Properties (JSON object)
    /// </summary>
    public string? Properties { get; set; }

    /// <summary>
    /// Clients that can access this resource
    /// </summary>
    public List<ClientResource> ClientResources { get; set; } = [];
}
