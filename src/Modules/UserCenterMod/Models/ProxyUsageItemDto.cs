namespace UserCenterMod.Models;

/// <summary>
/// Proxy usage list item.
/// </summary>
public class ProxyUsageItemDto
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public DateOnly Date { get; set; }

    public long Usage { get; set; }

    public DateTimeOffset CreatedTime { get; set; }

    public DateTimeOffset UpdatedTime { get; set; }
}
