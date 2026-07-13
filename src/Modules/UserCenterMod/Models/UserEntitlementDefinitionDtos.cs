namespace UserCenterMod.Models;

public class UserEntitlementDefinitionFilterDto : FilterBase
{
    public string? Keyword { get; set; }
}

public class UserEntitlementDefinitionUpsertDto
{
    [Required, MaxLength(200)]
    public required string DisplayName { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required, MaxLength(100)]
    public required string EntitlementCode { get; set; }

    public UserEntitlementType EntitlementType { get; set; }

    [Required, MaxLength(50)]
    public required string Unit { get; set; }
}

public class UserEntitlementDefinitionItemDto
{
    public Guid Id { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public required string EntitlementCode { get; set; }
    public UserEntitlementType EntitlementType { get; set; }
    public required string Unit { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
}
