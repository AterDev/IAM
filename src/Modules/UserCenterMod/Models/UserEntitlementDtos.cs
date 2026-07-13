namespace UserCenterMod.Models;

public class UserEntitlementFilterDto : FilterBase
{
    public Guid UserId { get; set; }
}

public class UserEntitlementAddDto
{
    public Guid EntitlementDefinitionId { get; set; }
    [Range(0, long.MaxValue)]
    public long ValueLimit { get; set; }
    public DateTimeOffset? ExpirationDate { get; set; }
    public DateTimeOffset StartDate { get; set; } = DateTimeOffset.UtcNow;
}

public class UserEntitlementUpdateDto
{
    [Range(0, long.MaxValue)]
    public long ValueLimit { get; set; }
    public DateTimeOffset? ExpirationDate { get; set; }
    public DateTimeOffset StartDate { get; set; }
}

public class UserEntitlementDetailDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid EntitlementDefinitionId { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public required string EntitlementCode { get; set; }
    public UserEntitlementType EntitlementType { get; set; }
    public required string Unit { get; set; }
    public long ValueLimit { get; set; }
    public long CurrentValue { get; set; }
    public DateTimeOffset? ExpirationDate { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
}
