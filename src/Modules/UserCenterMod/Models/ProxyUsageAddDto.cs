namespace UserCenterMod.Models;

/// <summary>
/// Proxy traffic usage increment request.
/// </summary>
public class ProxyUsageAddDto
{
    /// <summary>
    /// Traffic usage in bytes.
    /// </summary>
    public long Usage { get; set; }
}
