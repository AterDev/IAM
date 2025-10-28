namespace Entity;

/// <summary>
/// Scope claim entity
/// </summary>
[Module(Modules.Access)]
public class ScopeClaim : EntityBase
{
    /// <summary>
    /// Scope ID
    /// </summary>
    public Guid ScopeId { get; set; }

    /// <summary>
    /// Claim type
    /// </summary>
    [MaxLength(256)]
    public required string Type { get; set; }

    /// <summary>
    /// Scope navigation
    /// </summary>
    public ApiScope Scope { get; set; } = null!;
}
