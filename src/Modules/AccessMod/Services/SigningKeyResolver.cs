using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Perigon.AspNetCore.Services;
using System.Security.Cryptography;

namespace AccessMod.Services;

public sealed class SigningKeyResolver(
    CacheService cacheService,
    IServiceScopeFactory scopeFactory,
    ILogger<SigningKeyResolver> logger
    )
{
    private const string CacheKey = SigningKeyCacheKeys.Resolver;

    public async Task PreloadSigningKeysAsync(CancellationToken cancellationToken = default)
    {
        var signingKeys = await LoadSigningKeysFromDbAsync(cancellationToken);
        await cacheService.SetValueAsync(CacheKey, signingKeys);
    }

    public IEnumerable<SecurityKey> Resolve(string? keyId)
    {
        var signingKeys = cacheService.GetValueAsync<List<SigningKey>>(CacheKey).GetAwaiter().GetResult();
        var keys = signingKeys == null || signingKeys.Count == 0
            ? []
            : ConvertToSecurityKeys(signingKeys);

        if (keys == null || keys.Count == 0)
        {
            logger.LogWarning("No signing keys found while resolving JWT tokens.");
            return Enumerable.Empty<SecurityKey>();
        }

        if (string.IsNullOrEmpty(keyId))
        {
            return keys;
        }

        var matches = keys.Where(key => key.KeyId == keyId).ToList();
        if (matches.Count == 0)
        {
            logger.LogDebug("No cached signing key matched kid {KeyId}.", keyId);
            return keys;
        }

        return matches;
    }

    private async Task<List<SigningKey>> LoadSigningKeysFromDbAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DefaultDbContext>();
        var now = DateTimeOffset.UtcNow;
        return await dbContext.SigningKeys
            .Where(k =>
                !k.IsDeleted &&
                k.ActivationDate <= now &&
                (k.ExpirationDate == null || k.ExpirationDate > now))
            .OrderByDescending(k => k.CreatedTime)
            .Take(3)
            .ToListAsync(cancellationToken);
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
            logger.LogWarning("Loaded signing keys but none could be converted to security keys.");
        }

        return securityKeys;
    }

    private static SecurityKey? ConvertToSecurityKey(SigningKey signingKey)
    {
        if (string.IsNullOrWhiteSpace(signingKey.PublicKey))
        {
            return null;
        }

        try
        {
            using var rsa = RSA.Create();
            var publicKeyBytes = Convert.FromBase64String(signingKey.PublicKey);
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
        catch (CryptographicException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
