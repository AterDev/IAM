using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Entity.IAMMod;
using EntityFramework.AppDbContext;
using EntityFramework.AppDbFactory;
using IAMMod.Models.AccountDtos;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Perigon.AspNetCore.Abstraction;
using Perigon.AspNetCore.Utils;
using Share.Exceptions;
using Share.Implement;

namespace IAMMod.Managers;

/// <summary>
/// Handles MFA setup, verification and recovery-code lifecycle.
/// </summary>
public class MfaManager(
    TenantDbFactory dbContextFactory,
    IUserContext userContext,
    ILogger<MfaManager> logger,
    MfaTotpService mfaTotpService,
    AuditLogManager auditLogManager
) : ManagerBase<DefaultDbContext, User>(dbContextFactory, userContext, logger)
{
    private const string LoginProvider = "Mfa";
    private const string PendingSecretTokenName = "TotpPendingSecret";
    private const string ActiveSecretTokenName = "TotpSecret";
    private const string RecoveryCodesTokenName = "TotpRecoveryCodes";
    private readonly MfaTotpService _mfaTotpService = mfaTotpService;
    private readonly AuditLogManager _auditLogManager = auditLogManager;

    public override Task<bool> HasPermissionAsync(Guid id)
    {
        return Task.FromResult(_userContext.IsAdmin || _userContext.UserId == id);
    }

    public async Task<MfaStatusDto> GetStatusAsync(Guid userId)
    {
        var user = await GetUserAsync(userId);
        var pendingSetup = await GetUserTokenAsync(userId, PendingSecretTokenName);
        var recoveryCodes = await ReadRecoveryCodesAsync(userId);

        return new MfaStatusDto
        {
            IsEnabled = user.IsTwoFactorEnabled,
            HasPendingSetup = pendingSetup != null,
            RecoveryCodesRemaining = recoveryCodes.Count(code => !code.UsedAt.HasValue),
        };
    }

    public async Task<MfaSetupResponseDto> BeginSetupAsync(Guid userId, string issuer)
    {
        var user = await GetUserAsync(userId);
        var secret = _mfaTotpService.GenerateSecret();
        var accountName = string.IsNullOrWhiteSpace(user.Email) ? user.UserName : user.Email!;
        var payload = new TotpSecretPayload(secret, issuer, accountName, DateTimeOffset.UtcNow);

        await UpsertUserTokenAsync(userId, PendingSecretTokenName, JsonSerializer.Serialize(payload));
        await _auditLogManager.AddAuditLogAsync(
            category: "Authentication",
            eventName: "MfaSetupStarted",
            subjectId: userId.ToString(),
            payload: JsonSerializer.Serialize(new { issuer, accountName })
        );

        return new MfaSetupResponseDto
        {
            Secret = secret,
            OtpAuthUri = _mfaTotpService.BuildOtpAuthUri(issuer, accountName, secret),
            Issuer = issuer,
            AccountName = accountName,
        };
    }

    public async Task<MfaRecoveryCodesResponseDto> EnableAsync(Guid userId, string code)
    {
        var user = await GetUserAsync(userId);
        var pendingToken = await GetUserTokenAsync(userId, PendingSecretTokenName);
        var pendingSecret = pendingToken?.Value == null
            ? null
            : JsonSerializer.Deserialize<TotpSecretPayload>(pendingToken.Value);

        if (pendingSecret == null)
        {
            throw new BusinessException(Localizer.BadRequest, StatusCodes.Status400BadRequest);
        }

        if (!_mfaTotpService.ValidateCode(pendingSecret.Secret, code))
        {
            await _auditLogManager.AddAuditLogAsync(
                category: "Authentication",
                eventName: "MfaEnableFailed",
                subjectId: userId.ToString(),
                payload: JsonSerializer.Serialize(new { reason = "InvalidCode" })
            );
            throw new BusinessException(Localizer.BadRequest, StatusCodes.Status400BadRequest);
        }

        var activeSecret = pendingSecret with { CreatedAt = DateTimeOffset.UtcNow };
        await UpsertUserTokenAsync(userId, ActiveSecretTokenName, JsonSerializer.Serialize(activeSecret));
        await RemoveUserTokenAsync(userId, PendingSecretTokenName);

        var recoveryCodes = CreateRecoveryCodes();
        await SaveRecoveryCodesAsync(userId, recoveryCodes.entries);

        user.IsTwoFactorEnabled = true;
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.UpdatedTime = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        await _auditLogManager.AddAuditLogAsync(
            category: "Authentication",
            eventName: "MfaEnabled",
            subjectId: userId.ToString(),
            payload: JsonSerializer.Serialize(new { recoveryCodeCount = recoveryCodes.plainCodes.Count })
        );

        return new MfaRecoveryCodesResponseDto
        {
            RecoveryCodes = recoveryCodes.plainCodes,
        };
    }

    public async Task DisableAsync(Guid userId, string code)
    {
        var user = await GetUserAsync(userId);
        var verified = await VerifyUserCodeAsync(userId, code, allowRecoveryCode: true);
        if (!verified)
        {
            throw new BusinessException(Localizer.BadRequest, StatusCodes.Status400BadRequest);
        }

        await RemoveUserTokenAsync(userId, ActiveSecretTokenName);
        await RemoveUserTokenAsync(userId, PendingSecretTokenName);
        await RemoveUserTokenAsync(userId, RecoveryCodesTokenName);

        user.IsTwoFactorEnabled = false;
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.UpdatedTime = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        await _auditLogManager.AddAuditLogAsync(
            category: "Authentication",
            eventName: "MfaDisabled",
            subjectId: userId.ToString(),
            payload: JsonSerializer.Serialize(new { disabledAt = DateTimeOffset.UtcNow })
        );
    }

    public async Task<MfaRecoveryCodesResponseDto> RegenerateRecoveryCodesAsync(Guid userId, string code)
    {
        _ = await GetUserAsync(userId);
        var verified = await VerifyUserCodeAsync(userId, code, allowRecoveryCode: false);
        if (!verified)
        {
            throw new BusinessException(Localizer.BadRequest, StatusCodes.Status400BadRequest);
        }

        var recoveryCodes = CreateRecoveryCodes();
        await SaveRecoveryCodesAsync(userId, recoveryCodes.entries);
        await _dbContext.SaveChangesAsync();

        await _auditLogManager.AddAuditLogAsync(
            category: "Authentication",
            eventName: "MfaRecoveryCodesRegenerated",
            subjectId: userId.ToString(),
            payload: JsonSerializer.Serialize(new { recoveryCodeCount = recoveryCodes.plainCodes.Count })
        );

        return new MfaRecoveryCodesResponseDto
        {
            RecoveryCodes = recoveryCodes.plainCodes,
        };
    }

    public Task<bool> VerifyLoginChallengeAsync(Guid userId, string code)
    {
        return VerifyUserCodeAsync(userId, code, allowRecoveryCode: true);
    }

    private async Task<User> GetUserAsync(Guid userId)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(q => q.Id == userId && !q.IsDeleted);
        return user ?? throw new BusinessException(Localizer.UserNotFound, StatusCodes.Status404NotFound);
    }

    private async Task<bool> VerifyUserCodeAsync(Guid userId, string code, bool allowRecoveryCode)
    {
        var activeToken = await GetUserTokenAsync(userId, ActiveSecretTokenName);
        var activeSecret = activeToken?.Value == null
            ? null
            : JsonSerializer.Deserialize<TotpSecretPayload>(activeToken.Value);

        if (activeSecret != null && _mfaTotpService.ValidateCode(activeSecret.Secret, code))
        {
            return true;
        }

        if (!allowRecoveryCode)
        {
            return false;
        }

        var recoveryCodes = await ReadRecoveryCodesAsync(userId);
        if (recoveryCodes.Count == 0)
        {
            return false;
        }

        var normalizedCode = MfaTotpService.NormalizeCode(code);
        var matched = recoveryCodes.FirstOrDefault(entry =>
            !entry.UsedAt.HasValue && HashCrypto.Validate(normalizedCode, entry.Salt, entry.Hash));

        if (matched == null)
        {
            return false;
        }

        matched.UsedAt = DateTimeOffset.UtcNow;
        await SaveRecoveryCodesAsync(userId, recoveryCodes);
        await _dbContext.SaveChangesAsync();

        await _auditLogManager.AddAuditLogAsync(
            category: "Authentication",
            eventName: "MfaRecoveryCodeUsed",
            subjectId: userId.ToString(),
            payload: JsonSerializer.Serialize(new { remaining = recoveryCodes.Count(entry => !entry.UsedAt.HasValue) })
        );

        return true;
    }

    private async Task<List<RecoveryCodeEntry>> ReadRecoveryCodesAsync(Guid userId)
    {
        var token = await GetUserTokenAsync(userId, RecoveryCodesTokenName);
        if (string.IsNullOrWhiteSpace(token?.Value))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<RecoveryCodeEntry>>(token.Value) ?? [];
    }

    private async Task SaveRecoveryCodesAsync(Guid userId, List<RecoveryCodeEntry> entries)
    {
        await UpsertUserTokenAsync(userId, RecoveryCodesTokenName, JsonSerializer.Serialize(entries));
    }

    private (List<string> plainCodes, List<RecoveryCodeEntry> entries) CreateRecoveryCodes()
    {
        var plainCodes = new List<string>();
        var entries = new List<RecoveryCodeEntry>();

        for (var i = 0; i < 10; i++)
        {
            var plainCode = BuildRecoveryCode();
            plainCodes.Add(plainCode);

            var normalizedCode = MfaTotpService.NormalizeCode(plainCode);
            var salt = HashCrypto.BuildSalt();
            entries.Add(new RecoveryCodeEntry(
                HashCrypto.GeneratePwd(normalizedCode, salt),
                salt,
                null,
                plainCode[^4..]
            ));
        }

        return (plainCodes, entries);
    }

    private static string BuildRecoveryCode()
    {
        Span<byte> bytes = stackalloc byte[5];
        RandomNumberGenerator.Fill(bytes);
        var code = Convert.ToHexString(bytes);
        return $"{code[..5]}-{code[5..]}";
    }

    private async Task<UserToken?> GetUserTokenAsync(Guid userId, string name)
    {
        return await _dbContext.UserTokens.FirstOrDefaultAsync(q =>
            q.UserId == userId && q.LoginProvider == LoginProvider && q.Name == name);
    }

    private async Task UpsertUserTokenAsync(Guid userId, string name, string value)
    {
        var token = await GetUserTokenAsync(userId, name);
        if (token == null)
        {
            token = new UserToken
            {
                UserId = userId,
                LoginProvider = LoginProvider,
                Name = name,
                Value = value,
            };
            await _dbContext.UserTokens.AddAsync(token);
        }
        else
        {
            token.Value = value;
            token.UpdatedTime = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();
    }

    private async Task RemoveUserTokenAsync(Guid userId, string name)
    {
        var token = await GetUserTokenAsync(userId, name);
        if (token == null)
        {
            return;
        }

        _dbContext.UserTokens.Remove(token);
        await _dbContext.SaveChangesAsync();
    }

    private sealed record TotpSecretPayload(
        string Secret,
        string Issuer,
        string AccountName,
        DateTimeOffset CreatedAt
    );

    private sealed class RecoveryCodeEntry
    {
        public RecoveryCodeEntry(string hash, string salt, DateTimeOffset? usedAt, string suffix)
        {
            Hash = hash;
            Salt = salt;
            UsedAt = usedAt;
            Suffix = suffix;
        }

        public string Hash { get; set; }

        public string Salt { get; set; }

        public DateTimeOffset? UsedAt { get; set; }

        public string Suffix { get; set; }
    }
}
