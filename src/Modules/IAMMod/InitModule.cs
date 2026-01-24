using IAMMod.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IAMMod;

/// <summary>
/// AccessMod 模块初始化逻辑
/// </summary>
public static class InitModule
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DefaultDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DefaultDbContext>>();

        try
        {
            var now = DateTimeOffset.UtcNow;
            var hasActiveKey = await dbContext.SigningKeys
                .AnyAsync(k => k.IsActive && k.ActivationDate <= now && (k.ExpirationDate == null || k.ExpirationDate > now));

            if (!hasActiveKey)
            {
                logger.LogInformation("No active signing key found, generating initial key...");
                var (publicKey, privateKey) = HashCrypto.GenerateRsaKeyPair(2048);
                var signingKey = new SigningKey
                {
                    KeyId = Guid.CreateVersion7().ToString(),
                    Algorithm = "RS256",
                    KeyType = "RSA",
                    PrivateKey = privateKey,
                    PublicKey = publicKey,
                    Usage = "signing",
                    ActivationDate = DateTimeOffset.UtcNow,
                    ExpirationDate = DateTimeOffset.UtcNow.AddDays(365),
                    IsActive = true,
                    IsDeleted = false
                };

                dbContext.SigningKeys.Add(signingKey);
                await dbContext.SaveChangesAsync();

                logger.LogInformation("Initial signing key generated: {KeyId}", signingKey.KeyId);
            }

            // Preload signing keys into cache for JWT validation
            var signingKeyResolver = scope.ServiceProvider.GetRequiredService<SigningKeyResolver>();
            await signingKeyResolver.PreloadSigningKeysAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initializing signing keys");
        }
    }
}
