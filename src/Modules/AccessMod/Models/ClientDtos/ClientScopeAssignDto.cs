namespace AccessMod.Models.ClientDtos;

/// <summary>
/// Client scope assignment DTO
/// </summary>
public class ClientScopeAssignDto
{
    /// <summary>
    /// List of scope IDs to assign to the client
    /// </summary>
    public List<Guid> ScopeIds { get; set; } = [];
}
