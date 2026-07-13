namespace Entity.UserCenterMod;

/// <summary>
/// Defines an entitlement that can be assigned to users.
/// </summary>
public class UserEntitlementDefinition : EntityBase
{
    [MaxLength(200)]
    public required string DisplayName { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public required string EntitlementCode { get; set; }

    public UserEntitlementType EntitlementType { get; set; }

    [MaxLength(50)]
    public required string Unit { get; set; }

    public List<UserEntitlement> UserEntitlements { get; set; } = [];
}
