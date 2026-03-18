namespace IAMMod.Services;

/// <summary>
/// Risk control and anti-abuse policy settings.
/// </summary>
public class RiskControlOption
{
    public const string ConfigPath = "RiskControl";

    /// <summary>
    /// Maximum failed password attempts before an account lockout is applied.
    /// </summary>
    public int LoginFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Sliding window in seconds for login failure counters.
    /// </summary>
    public int LoginFailureWindowSeconds { get; set; } = 300;

    /// <summary>
    /// Account lockout duration in minutes.
    /// </summary>
    public int AccountLockoutMinutes { get; set; } = 30;

    /// <summary>
    /// Number of recent successful sessions used to evaluate familiar IP and user-agent values.
    /// </summary>
    public int KnownSessionLookbackCount { get; set; } = 3;

    /// <summary>
    /// Maximum device-code polling attempts allowed within the configured window.
    /// </summary>
    public int DeviceCodePollLimit { get; set; } = 5;

    /// <summary>
    /// Sliding window in seconds for device-code polling protection.
    /// </summary>
    public int DeviceCodePollWindowSeconds { get; set; } = 30;
}