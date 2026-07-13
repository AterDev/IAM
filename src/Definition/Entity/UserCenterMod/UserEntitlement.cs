namespace Entity.UserCenterMod;

/// <summary>
/// A user's assigned entitlement.
/// </summary>
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
