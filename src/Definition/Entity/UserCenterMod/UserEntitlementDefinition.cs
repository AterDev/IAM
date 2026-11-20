namespace Entity.UserCenterMod;

/// <summary>
/// Defines an entitlement that can be assigned to users.
/// </summary>
public class UserEntitlementDefinition : EntityBase
{
    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(200)")]
    public string DisplayName { get; set; }

    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(1000)")]
    public string? Description { get; set; }

    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(100)")]
    public string EntitlementCode { get; set; }

    public UserEntitlementType EntitlementType { get; set; }

    [Perigon.PostgreSQL.Attributes.Column(TypeName = "character varying(50)")]
    public string Unit { get; set; }

    public List<UserEntitlement> UserEntitlements { get; set; } = [];
}
