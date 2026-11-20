namespace Entity.UserCenterMod;

/// <summary>
/// A user's assigned entitlement.
/// </summary>
[Perigon.PostgreSQL.Attributes.Index(nameof(UserEntitlement.UserId), nameof(UserEntitlement.EntitlementDefinitionId), IsUnique = true)]
public class UserEntitlement : EntityBase
{
    public Guid UserId { get; set; }
    public Guid EntitlementDefinitionId { get; set; }
    public UserEntitlementDefinition? EntitlementDefinition { get; set; }
    public long ValueLimit { get; set; }
    public long CurrentValue { get; set; }
    public DateTimeOffset? ExpirationDate { get; set; }
    public DateTimeOffset StartDate { get; set; }
}
