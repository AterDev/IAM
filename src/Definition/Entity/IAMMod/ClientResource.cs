namespace Entity.IAMMod;

/// <summary>
/// Client and API Resource many-to-many relationship entity
/// </summary>
/// <remarks>
/// Maps which API resources a client is allowed to access.
/// When a client requests a token with a specific resource,
/// the authorization server verifies the client has access via this relationship.
/// </remarks>
public class ClientResource : EntityBase
{
    /// <summary>
    /// Client ID
    /// </summary>
    public Guid ClientId { get; set; }

    /// <summary>
    /// API Resource ID
    /// </summary>
    public Guid ApiResourceId { get; set; }

    /// <summary>
    /// Client navigation property
    /// </summary>
    public Client Client { get; set; } = null!;

    /// <summary>
    /// API Resource navigation property
    /// </summary>
    public ApiResource ApiResource { get; set; } = null!;
}
