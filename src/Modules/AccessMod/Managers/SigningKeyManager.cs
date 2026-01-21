using Entity.CommonMod;
using Perigon.AspNetCore.Services;

namespace AccessMod.Managers;

/// <summary>
/// 签名密钥管理器 - 处理密钥相关的数据库业务逻辑 (AccessMod 层)
/// 负责密钥的获取、生成、轮转等维护任务
/// </summary>
public class SigningKeyManager(
    TenantDbFactory dbContextFactory,
    IUserContext userContext,
    CacheService cacheService,
    ILogger<SigningKeyManager> logger
) : ManagerBase<DefaultDbContext, SigningKey>(dbContextFactory, userContext, logger)
{
    private readonly CacheService _cacheService = cacheService;
    private const string ActiveKeyCacheKey = "SigningKey:Active";

    public override Task<bool> HasPermissionAsync(Guid id)
    {
        // Signing key management is restricted to the platform, deny by default and extend later if needed
        return Task.FromResult(false);
    }

    /// <summary>
    /// 获取当前活跃的签名密钥
    /// </summary>
    public async Task<SigningKey?> GetActiveSigningKeyAsync(CancellationToken cancellationToken = default)
    {
        return await _cacheService.GetOrCreateAsync(ActiveKeyCacheKey, async ct =>
            {
                var now = DateTimeOffset.UtcNow;
                return await _dbSet
                    .Where(k =>
                        k.IsActive &&
                        !k.IsDeleted &&
                        k.ActivationDate <= now &&
                        (k.ExpirationDate == null || k.ExpirationDate > now))
                    .OrderByDescending(k => k.CreatedTime)
                    .FirstOrDefaultAsync(ct);
            },
            cancellationToken
        );
    }

    /// <summary>
    /// 获取所有有效的公钥（用于 JWKS 端点或 Token 验证）
    /// </summary>
    public async Task<List<SigningKey>> GetValidPublicKeysAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        return await _dbSet
            .Where(k =>
                !k.IsDeleted &&
                k.ActivationDate <= now &&
                (k.ExpirationDate == null || k.ExpirationDate > now))
            .OrderByDescending(k => k.CreatedTime)
            .Take(3)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 生成新的 RSA 密钥对并保存到数据库
    /// </summary>
    public async Task<SigningKey> GenerateNewKeyAsync(string algorithm = "RS256", CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating new signing key with algorithm {Algorithm}", algorithm);

        var (publicKey, privateKey) = HashCrypto.GenerateRsaKeyPair(2048);

        var signingKey = new SigningKey
        {
            KeyId = Guid.CreateVersion7().ToString(),
            Algorithm = algorithm,
            KeyType = "RSA",
            PrivateKey = privateKey,
            PublicKey = publicKey,
            Usage = "signing",
            ActivationDate = DateTimeOffset.UtcNow,
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(365),
            IsActive = true,
            IsDeleted = false,
            CreatedTime = DateTimeOffset.UtcNow,
            UpdatedTime = DateTimeOffset.UtcNow,
        };

        await _dbSet.AddAsync(signingKey, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cacheService.RemoveAsync(ActiveKeyCacheKey);

        return signingKey;
    }

    /// <summary>
    /// 撤销密钥
    /// </summary>
    public async Task<bool> RevokeKeyAsync(Guid keyId, CancellationToken cancellationToken = default)
    {
        var key = await _dbSet.FindAsync([keyId], cancellationToken);
        if (key == null)
        {
            return false;
        }

        key.IsActive = false;
        key.IsDeleted = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cacheService.RemoveAsync(ActiveKeyCacheKey);

        return true;
    }
}
