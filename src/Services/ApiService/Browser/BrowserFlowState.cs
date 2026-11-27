using System.Security.Cryptography;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;

namespace ApiService.Browser;

internal sealed record PendingMfaState(
    Guid UserId,
    string UserName,
    bool RememberMe,
    string? ReturnUrl,
    DateTimeOffset ExpiresAt);

public sealed class BrowserFlowState(
    IDataProtectionProvider dataProtectionProvider,
    IMemoryCache memoryCache)
{
    internal const string PendingMfaCookieName = "iam.pending-mfa";

    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("IAM.BrowserFlow.v1");
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly ConcurrentDictionary<string, object> _consumeLocks = new(StringComparer.Ordinal);

    internal string Protect(PendingMfaState state)
    {
        var challengeId = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _memoryCache.Set(
            challengeId,
            state,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = state.ExpiresAt,
            });
        return _protector.Protect(challengeId);
    }

    internal bool TryUnprotect(string? protectedState, out PendingMfaState? state)
    {
        state = null;
        if (string.IsNullOrWhiteSpace(protectedState))
        {
            return false;
        }

        try
        {
            var challengeId = _protector.Unprotect(protectedState);
            if (!_memoryCache.TryGetValue<PendingMfaState>(challengeId, out var pendingState)
                || pendingState is null)
            {
                return false;
            }

            if (pendingState.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                _memoryCache.Remove(challengeId);
                return false;
            }

            state = pendingState;
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    internal bool TryConsume(string? protectedState, [NotNullWhen(true)] out PendingMfaState? state)
    {
        state = null;
        if (string.IsNullOrWhiteSpace(protectedState))
        {
            return false;
        }

        string? challengeId = null;
        try
        {
            challengeId = _protector.Unprotect(protectedState);
            var consumeLock = _consumeLocks.GetOrAdd(challengeId, static _ => new object());
            lock (consumeLock)
            {
                if (!_memoryCache.TryGetValue<PendingMfaState>(challengeId, out var pendingState)
                    || pendingState is null
                    || pendingState.ExpiresAt <= DateTimeOffset.UtcNow)
                {
                    _memoryCache.Remove(challengeId);
                    return false;
                }

                _memoryCache.Remove(challengeId);
                state = pendingState;
                return true;
            }
        }
        catch (CryptographicException)
        {
            return false;
        }
        finally
        {
            if (challengeId is not null)
            {
                _consumeLocks.TryRemove(challengeId, out _);
            }
        }
    }
}
