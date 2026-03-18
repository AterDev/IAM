using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace IAMMod.Services;

/// <summary>
/// Lightweight configurable risk control service for login anomalies and device polling protection.
/// </summary>
public class RiskControlService(
    DefaultDbContext dbContext,
    IMemoryCache memoryCache,
    IOptions<RiskControlOption> options,
    ILogger<RiskControlService> logger)
{
    private readonly DefaultDbContext _dbContext = dbContext;
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly ILogger<RiskControlService> _logger = logger;
    private readonly RiskControlOption _options = options.Value;

    public int LoginFailureThreshold => Math.Max(1, _options.LoginFailureThreshold);

    public TimeSpan AccountLockoutDuration => TimeSpan.FromMinutes(Math.Max(1, _options.AccountLockoutMinutes));

    /// <summary>
    /// Evaluate whether a login attempt looks unfamiliar compared to recent successful sessions.
    /// </summary>
    public async Task<LoginRiskAssessment> EvaluateLoginRiskAsync(
        Guid userId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var recentSessions = await _dbContext.LoginSessions
            .Where(q => q.UserId == userId && q.IsActive)
            .OrderByDescending(q => q.LastActivityTime)
            .Take(Math.Max(1, _options.KnownSessionLookbackCount))
            .ToListAsync(cancellationToken);

        var recentIpAddresses = recentSessions
            .Select(q => q.IpAddress)
            .Where(static q => !string.IsNullOrWhiteSpace(q))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();

        var recentUserAgents = recentSessions
            .Select(q => q.UserAgent)
            .Where(static q => !string.IsNullOrWhiteSpace(q))
            .Distinct(StringComparer.Ordinal)
            .Cast<string>()
            .ToList();

        var hasKnownIp = string.IsNullOrWhiteSpace(ipAddress)
            || recentIpAddresses.Count == 0
            || recentIpAddresses.Contains(ipAddress, StringComparer.OrdinalIgnoreCase);

        var hasKnownUserAgent = string.IsNullOrWhiteSpace(userAgent)
            || recentUserAgents.Count == 0
            || recentUserAgents.Contains(userAgent, StringComparer.Ordinal);

        return new LoginRiskAssessment(
            HasKnownIp: hasKnownIp,
            HasKnownUserAgent: hasKnownUserAgent,
            RequiresStepUp: recentSessions.Count > 0 && (!hasKnownIp || !hasKnownUserAgent),
            LastKnownIpAddress: recentIpAddresses.FirstOrDefault(),
            LastKnownUserAgent: recentUserAgents.FirstOrDefault());
    }

    /// <summary>
    /// Register a failed login attempt in a sliding memory window.
    /// </summary>
    public LoginFailureTrackingResult RegisterLoginFailure(string userName, Guid? userId, string? ipAddress)
    {
        var userKey = BuildLoginUserFailureKey(userId, userName);
        var ipKey = BuildLoginIpFailureKey(ipAddress);
        var userWindow = TimeSpan.FromSeconds(Math.Max(10, _options.LoginFailureWindowSeconds));

        var userFailureCount = IncrementWindowCounter(userKey, userWindow);
        var ipFailureCount = string.IsNullOrWhiteSpace(ipKey)
            ? 0
            : IncrementWindowCounter(ipKey, userWindow);

        return new LoginFailureTrackingResult(userFailureCount, ipFailureCount);
    }

    /// <summary>
    /// Clear remembered failed login state after a successful sign-in.
    /// </summary>
    public void ResetLoginFailures(string userName, Guid userId, string? ipAddress)
    {
        _memoryCache.Remove(BuildLoginUserFailureKey(userId, userName));

        var ipKey = BuildLoginIpFailureKey(ipAddress);
        if (!string.IsNullOrWhiteSpace(ipKey))
        {
            _memoryCache.Remove(ipKey);
        }
    }

    /// <summary>
    /// Register a device-code polling attempt and indicate whether the caller should be slowed down.
    /// </summary>
    public DevicePollingAssessment RegisterDeviceCodePoll(string? clientId, string deviceCode)
    {
        var window = TimeSpan.FromSeconds(Math.Max(5, _options.DeviceCodePollWindowSeconds));
        var count = IncrementWindowCounter(BuildDeviceCodePollKey(clientId, deviceCode), window);
        var blocked = count > Math.Max(1, _options.DeviceCodePollLimit);
        DateTimeOffset? blockedUntil = blocked ? DateTimeOffset.UtcNow.Add(window) : null;

        if (blocked)
        {
            _logger.LogWarning(
                "Device code polling limit reached for client {ClientId} and device code {DeviceCode}",
                clientId,
                deviceCode);
        }

        return new DevicePollingAssessment(count, blocked, blockedUntil);
    }

    private int IncrementWindowCounter(string cacheKey, TimeSpan window)
    {
        var now = DateTimeOffset.UtcNow;

        if (!_memoryCache.TryGetValue<SlidingWindowCounter>(cacheKey, out var state)
            || state == null
            || now - state.WindowStartedAt >= window)
        {
            state = new SlidingWindowCounter
            {
                WindowStartedAt = now,
                Count = 0,
            };
        }

        state.Count++;
        _memoryCache.Set(cacheKey, state, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = window,
        });

        return state.Count;
    }

    private static string BuildLoginUserFailureKey(Guid? userId, string userName)
    {
        var normalized = userId?.ToString("N") ?? userName.Trim().ToUpperInvariant();
        return $"risk:login:user:{normalized}";
    }

    private static string? BuildLoginIpFailureKey(string? ipAddress)
    {
        return string.IsNullOrWhiteSpace(ipAddress)
            ? null
            : $"risk:login:ip:{ipAddress.Trim()}";
    }

    private static string BuildDeviceCodePollKey(string? clientId, string deviceCode)
    {
        var normalizedClientId = string.IsNullOrWhiteSpace(clientId) ? "anonymous" : clientId.Trim();
        return $"risk:device:poll:{normalizedClientId}:{deviceCode.Trim()}";
    }

    private sealed class SlidingWindowCounter
    {
        public DateTimeOffset WindowStartedAt { get; set; }

        public int Count { get; set; }
    }
}

/// <summary>
/// Login anomaly evaluation result.
/// </summary>
public sealed record LoginRiskAssessment(
    bool HasKnownIp,
    bool HasKnownUserAgent,
    bool RequiresStepUp,
    string? LastKnownIpAddress,
    string? LastKnownUserAgent);

/// <summary>
/// Failed-login sliding window counters.
/// </summary>
public sealed record LoginFailureTrackingResult(int UserFailureCount, int IpFailureCount);

/// <summary>
/// Device-code polling risk evaluation result.
/// </summary>
public sealed record DevicePollingAssessment(int AttemptCount, bool IsBlocked, DateTimeOffset? BlockedUntil);