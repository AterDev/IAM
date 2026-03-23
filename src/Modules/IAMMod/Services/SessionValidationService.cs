using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Perigon.AspNetCore.Services;

namespace IAMMod.Services;

/// <summary>
/// Cache-backed session validation and activity synchronization service.
/// </summary>
public class SessionValidationService(
    DefaultDbContext dbContext,
    CacheService cacheService,
    IOptions<JwtOption> jwtOptions)
{
    private static readonly TimeSpan ActivitySyncInterval = TimeSpan.FromMinutes(5);
    private const int MinimumCacheExpirationSeconds = 1;

    private readonly DefaultDbContext _dbContext = dbContext;
    private readonly CacheService _cacheService = cacheService;
    private readonly JwtOption _jwtOptions = jwtOptions.Value;

    public async Task<bool> ValidateAsync(
        Guid userId,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        var state = await GetStateAsync(userId, sessionId, cancellationToken);
        if (state is null)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        if (!state.IsActive || (state.ExpirationTime.HasValue && state.ExpirationTime <= now))
        {
            if (state.IsActive)
            {
                state.IsActive = false;
                await DeactivateInDatabaseAsync(userId, sessionId, cancellationToken);
                state.LastPersistedActivityTime = now;
            }

            await SetStateAsync(state);
            return false;
        }

        state.LastActivityTime = now;

        if (now - state.LastPersistedActivityTime >= ActivitySyncInterval)
        {
            var session = await _dbContext.LoginSessions.FirstOrDefaultAsync(
                q => q.UserId == userId && q.SessionId == sessionId,
                cancellationToken);

            if (session is null)
            {
                await SetStateAsync(null, userId, sessionId);
                return false;
            }

            if (!session.IsActive || (session.ExpirationTime.HasValue && session.ExpirationTime <= now))
            {
                if (session.IsActive)
                {
                    session.IsActive = false;
                    session.UpdatedTime = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                state.IsActive = false;
                state.ExpirationTime = session.ExpirationTime;
                state.LastPersistedActivityTime = session.LastActivityTime;
                await SetStateAsync(state);
                return false;
            }

            session.LastActivityTime = now;
            session.UpdatedTime = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            state.LastPersistedActivityTime = now;
        }

        await SetStateAsync(state);
        return true;
    }

    public Task SetStateAsync(LoginSession session)
    {
        return SetStateAsync(SessionValidationCacheEntry.FromSession(session));
    }

    public Task SetStateAsync(SessionValidationCacheEntry state)
    {
        return SetStateAsync(state, state.UserId, state.SessionId);
    }

    public Task RemoveAsync(Guid userId, string sessionId)
    {
        return _cacheService.RemoveAsync(GetCacheKey(userId, sessionId));
    }

    private Task<SessionValidationCacheEntry?> GetStateAsync(
        Guid userId,
        string sessionId,
        CancellationToken cancellationToken)
    {
        return _cacheService.GetOrCreateWithExpirationAsync<SessionValidationCacheEntry?>(
            GetCacheKey(userId, sessionId),
            async ct => await _dbContext.LoginSessions
                .AsNoTracking()
                .Where(q => q.UserId == userId && q.SessionId == sessionId)
                .Select(q => new SessionValidationCacheEntry
                {
                    UserId = q.UserId,
                    SessionId = q.SessionId,
                    IsActive = q.IsActive,
                    LastActivityTime = q.LastActivityTime,
                    LastPersistedActivityTime = q.LastActivityTime,
                    ExpirationTime = q.ExpirationTime,
                })
                .FirstOrDefaultAsync(ct),
            expiration: _jwtOptions.ExpiredSecond,
            cancellation: cancellationToken);
    }

    private async Task DeactivateInDatabaseAsync(
        Guid userId,
        string sessionId,
        CancellationToken cancellationToken)
    {
        var session = await _dbContext.LoginSessions.FirstOrDefaultAsync(
            q => q.UserId == userId && q.SessionId == sessionId,
            cancellationToken);

        if (session is null || !session.IsActive)
        {
            return;
        }

        session.IsActive = false;
        session.UpdatedTime = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private Task SetStateAsync(
        SessionValidationCacheEntry? state,
        Guid userId,
        string sessionId)
    {
        return _cacheService.SetValueAsync(
            GetCacheKey(userId, sessionId),
            state,
            GetCacheExpirationSeconds(state?.ExpirationTime));
    }

    private int GetCacheExpirationSeconds(DateTimeOffset? expirationTime)
    {
        if (!expirationTime.HasValue)
        {
            return _jwtOptions.ExpiredSecond;
        }

        var remainingSeconds = (int)Math.Ceiling((expirationTime.Value - DateTimeOffset.UtcNow).TotalSeconds);
        return Math.Max(MinimumCacheExpirationSeconds, remainingSeconds);
    }

    private static string GetCacheKey(Guid userId, string sessionId)
    {
        return $"{nameof(SessionValidationService)}:{userId:N}:{sessionId}";
    }
}

/// <summary>
/// Cached session validation state.
/// </summary>
public class SessionValidationCacheEntry
{
    public Guid UserId { get; set; }

    public required string SessionId { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset LastActivityTime { get; set; }

    public DateTimeOffset LastPersistedActivityTime { get; set; }

    public DateTimeOffset? ExpirationTime { get; set; }

    public static SessionValidationCacheEntry FromSession(LoginSession session)
    {
        return new SessionValidationCacheEntry
        {
            UserId = session.UserId,
            SessionId = session.SessionId,
            IsActive = session.IsActive,
            LastActivityTime = session.LastActivityTime,
            LastPersistedActivityTime = session.LastActivityTime,
            ExpirationTime = session.ExpirationTime,
        };
    }
}