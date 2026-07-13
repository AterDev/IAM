using Entity.IAMMod;
using Perigon.AspNetCore.Services;
using Share.Exceptions;
using System.Text;
using UserCenterMod.Models;

namespace UserCenterMod.Managers;

/// <summary>
/// User center business operations.
/// </summary>
public class UserCenterManager(
    TenantDbFactory dbContextFactory,
    IUserContext userContext,
    ILogger<UserCenterManager> logger,
    CacheService cacheService
) : ManagerBase<DefaultDbContext, User>(dbContextFactory, userContext, logger)
{
    private const int TokenValidSeconds = 5;
    private const int UserCacheSeconds = 60 * 60;

    private readonly CacheService _cacheService = cacheService;

    public override Task<bool> HasPermissionAsync(Guid id)
    {
        return Task.FromResult(true);
    }

    public async Task<Guid> LoginAsync(UserCenterLoginDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            throw new BusinessException(Localizer.InvalidEmailOrPassword, StatusCodes.Status401Unauthorized);
        }

        var normalizedEmail = dto.Email.Trim().ToUpperInvariant();
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null
            || string.IsNullOrWhiteSpace(user.PasswordHash)
            || string.IsNullOrWhiteSpace(user.PasswordSalt)
            || !HashCrypto.Validate(dto.Password, user.PasswordSalt, user.PasswordHash))
        {
            throw new BusinessException(Localizer.InvalidEmailOrPassword, StatusCodes.Status401Unauthorized);
        }

        return user.Id;
    }

    public async Task<UserCenterTokenUser?> ValidateTokenAsync(
        string? authorization,
        CancellationToken cancellationToken = default
    )
    {
        var token = NormalizeAuthorization(authorization);
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        if (!TryParseToken(token, out var userId, out var email, out var timestamp))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(now - timestamp) > TokenValidSeconds)
        {
            return null;
        }

        var normalizedEmail = email.Trim().ToUpperInvariant();
        var cacheKey = $"user-center:user:{userId:N}:{normalizedEmail}";

        return await _cacheService.GetOrCreateWithExpirationAsync(
            cacheKey,
            async ct =>
            {
                var exists = await _dbContext.Users
                    .AsNoTracking()
                    .AnyAsync(
                        q => q.Id == userId && q.NormalizedEmail == normalizedEmail,
                        ct
                    );

                return exists ? new UserCenterTokenUser(userId, normalizedEmail) : null;
            },
            expiration: UserCacheSeconds,
            cancellation: cancellationToken
        );
    }

    private static string? NormalizeAuthorization(string? authorization)
    {
        if (string.IsNullOrWhiteSpace(authorization))
        {
            return null;
        }

        var value = authorization.Trim();
        return value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? value["Bearer ".Length..].Trim()
            : value;
    }

    private static bool TryParseToken(
        string token,
        out Guid userId,
        out string email,
        out long timestamp
    )
    {
        userId = Guid.Empty;
        email = string.Empty;
        timestamp = 0;

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var parts = decoded.Split("::", StringSplitOptions.None);
            if (parts.Length != 3)
            {
                return false;
            }

            email = parts[1];
            return Guid.TryParse(parts[0], out userId)
                && !string.IsNullOrWhiteSpace(email)
                && long.TryParse(parts[2], out timestamp);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public sealed record UserCenterTokenUser(Guid UserId, string NormalizedEmail);
