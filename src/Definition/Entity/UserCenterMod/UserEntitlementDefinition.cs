namespace Entity.UserCenterMod;

/// <summary>
/// Defines an entitlement that can be assigned to users.
/// </summary>
public class UserEntitlementDefinition : EntityBase
{
    public string DisplayName { get; set; }

    public string? Description { get; set; }

    public string EntitlementCode { get; set; }

    public UserEntitlementType EntitlementType { get; set; }

    public string Unit { get; set; }

    public List<UserEntitlement> UserEntitlements { get; set; } = [];
}
