using Entity.IAMMod;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Perigon.AspNetCore.Services;
using System.Security.Cryptography;

namespace IAMMod.Services;

/// <summary>
/// Resolves signing keys from database for local JWT token validation in auth center.
/// Uses async-first approach with fallback from cache to database.
/// </summary>
public sealed class SigningKeyResolver(
    CacheService cacheService,
    IServiceScopeFactory scopeFactory,
    ILogger<SigningKeyResolver> logger
)
{
    /// <summary>
    /// Preload signing keys from database into cache at startup.
    /// </summary>
    public async Task PreloadSigningKeysAsync(CancellationToken cancellationToken = default)
    {
        var signingKeys = await LoadSigningKeysFromDbAsync(cancellationToken);
        if (signingKeys.Count > 0)
        {
            await cacheService.SetValueAsync(SigningKeyCacheKey, signingKeys);
            logger.LogInformation("Preloaded {KeyCount} signing keys into cache", signingKeys.Count);
        }
        else
        {
            logger.LogWarning("No valid signing keys found in database during preload");
        }
    }

    /// <summary>
    /// Resolve signing keys asynchronously (primary method for JWT validation).
    /// </summary>
    public async Task<IReadOnlyList<SecurityKey>> ResolveAsync(
        string? keyId,
        CancellationToken cancellationToken = default
    )
    {
        // Try to get from cache first
        var signingKeys = await cacheService.GetValueAsync<List<SigningKey>>(SigningKeyCacheKey, cancellationToken);

        // If cache miss, load from database and repopulate cache
        if (signingKeys == null || signingKeys.Count == 0)
        {
            logger.LogDebug("Cache miss for signing keys, loading from database");
            signingKeys = await LoadSigningKeysFromDbAsync(cancellationToken);

            if (signingKeys.Count > 0)
            {
                await cacheService.SetValueAsync(SigningKeyCacheKey, signingKeys);
                logger.LogDebug("Repopulated cache with {KeyCount} signing keys from database", signingKeys.Count);
            }
        }

        // Convert to SecurityKey objects
        var keys = signingKeys == null || signingKeys.Count == 0
            ? []
            : ConvertToSecurityKeys(signingKeys);

        if (keys.Count == 0)
        {
            logger.LogWarning("No signing keys found or convertible to SecurityKey for JWT validation");
            return Array.Empty<SecurityKey>();
        }

        // Filter by kid if provided
        if (string.IsNullOrEmpty(keyId))
        {
            return keys;
        }

        var matches = keys.Where(key => key.KeyId == keyId).ToList();
        if (matches.Count == 0)
        {
            logger.LogDebug("No cached signing key matched kid {KeyId}, returning all available keys", keyId);
            return keys;
        }

        return matches;
    }

    /// <summary>
    /// Synchronous wrapper for backwards compatibility (delegates to async variant).
    /// WARNING: Calling async operation synchronously can cause issues in high-concurrency scenarios.
    /// Prefer ResolveAsync in request pipelines.
    /// </summary>
    public IEnumerable<SecurityKey> Resolve(string? keyId)
    {
        return ResolveAsync(keyId).GetAwaiter().GetResult();
    }


    private async Task<List<SigningKey>> LoadSigningKeysFromDbAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DefaultDbContext>();
        var now = DateTimeOffset.UtcNow;

        var keys = await dbContext.SigningKeys
            .Where(k =>
                !k.IsDeleted &&
                k.ActivationDate <= now &&
                (k.ExpirationDate == null || k.ExpirationDate > now))
            .OrderByDescending(k => k.CreatedTime)
            .Take(3)
            .ToListAsync(cancellationToken);

        logger.LogDebug("Loaded {KeyCount} active signing keys from database", keys.Count);
        return keys;
    }

    private IReadOnlyList<SecurityKey> ConvertToSecurityKeys(IEnumerable<SigningKey> signingKeys)
    {
        var securityKeys = signingKeys
            .Select(ConvertToSecurityKey)
            .Where(key => key != null)
            .Select(key => key!)
            .ToList();

        if (securityKeys.Count == 0)
        {
            logger.LogWarning("Loaded signing keys but none could be converted to SecurityKey");
        }

        return securityKeys;
    }

    private SecurityKey? ConvertToSecurityKey(SigningKey signingKey)
    {
        if (string.IsNullOrWhiteSpace(signingKey.PublicKey))
        {
            return null;
        }

        try
        {
            using var rsa = RSA.Create();
            var publicKeyBytes = Convert.FromBase64String(signingKey.PublicKey);

            // Validate minimum RSA key size (2048 bits = 256 bytes)
            if (publicKeyBytes.Length < 256)
            {
                return null;
            }

            rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);
            var parameters = rsa.ExportParameters(false);

            if (parameters.Modulus == null || parameters.Exponent == null)
            {
                return null;
            }

            return new RsaSecurityKey(parameters)
            {
                KeyId = signingKey.KeyId
            };
        }
        catch (CryptographicException ex)
        {
            logger.LogWarning(ex, "Failed to convert signing key {KeyId} to SecurityKey due to cryptographic error", signingKey.KeyId);
            return null;
        }
        catch (FormatException ex)
        {
            logger.LogWarning(ex, "Failed to convert signing key {KeyId} to SecurityKey due to invalid format", signingKey.KeyId);
            return null;
        }
    }
}
