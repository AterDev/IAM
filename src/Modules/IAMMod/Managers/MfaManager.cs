using IAMMod.Serialization;
using System.Text.Json.Nodes;
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
    AppDbFactory dbContextFactory,
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
        var payload = new MfaTotpSecretPayload(secret, issuer, accountName, DateTimeOffset.UtcNow);

        await UpsertUserTokenAsync(userId, PendingSecretTokenName, JsonSerializer.Serialize(payload, IamModJsonSerializerContext.Default.MfaTotpSecretPayload));
        await _auditLogManager.AddAuditLogAsync(
            category: "Authentication",
            eventName: "MfaSetupStarted",
            subjectId: userId.ToString(),
            payload: new JsonObject { ["issuer"] = issuer, ["accountName"] = accountName }.ToJsonString()
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
            : JsonSerializer.Deserialize(pendingToken.Value, IamModJsonSerializerContext.Default.MfaTotpSecretPayload);

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
                payload: new JsonObject { ["reason"] = "InvalidCode" }.ToJsonString()
            );
            throw new BusinessException(Localizer.BadRequest, StatusCodes.Status400BadRequest);
        }

        var activeSecret = pendingSecret with { CreatedAt = DateTimeOffset.UtcNow };
        await UpsertUserTokenAsync(userId, ActiveSecretTokenName, JsonSerializer.Serialize(activeSecret, IamModJsonSerializerContext.Default.MfaTotpSecretPayload));
        await RemoveUserTokenAsync(userId, PendingSecretTokenName);

        var recoveryCodes = CreateRecoveryCodes();
        await SaveRecoveryCodesAsync(userId, recoveryCodes.entries);

        user.IsTwoFactorEnabled = true;
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.UpdatedTime = DateTime.UtcNow;
        await PersistUserSecurityStateAsync(userId, true, user.SecurityStamp, user.UpdatedTime);

        await _auditLogManager.AddAuditLogAsync(
            category: "Authentication",
            eventName: "MfaEnabled",
            subjectId: userId.ToString(),
            payload: new JsonObject { ["recoveryCodeCount"] = recoveryCodes.plainCodes.Count }.ToJsonString()
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
        await PersistUserSecurityStateAsync(userId, false, user.SecurityStamp, user.UpdatedTime);

        await _auditLogManager.AddAuditLogAsync(
            category: "Authentication",
            eventName: "MfaDisabled",
            subjectId: userId.ToString(),
            payload: new JsonObject { ["disabledAt"] = DateTimeOffset.UtcNow }.ToJsonString()
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

        await _auditLogManager.AddAuditLogAsync(
            category: "Authentication",
            eventName: "MfaRecoveryCodesRegenerated",
            subjectId: userId.ToString(),
            payload: new JsonObject { ["recoveryCodeCount"] = recoveryCodes.plainCodes.Count }.ToJsonString()
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
        var user = await _dbContext.Users
            .Where(q => q.Id == userId && !q.IsDeleted)
            .FirstOrDefaultAsync();
        return user ?? throw new BusinessException(Localizer.UserNotFound, StatusCodes.Status404NotFound);
    }

    private async Task<bool> VerifyUserCodeAsync(Guid userId, string code, bool allowRecoveryCode)
    {
        var activeToken = await GetUserTokenAsync(userId, ActiveSecretTokenName);
        var activeSecret = activeToken?.Value == null
            ? null
            : JsonSerializer.Deserialize(activeToken.Value, IamModJsonSerializerContext.Default.MfaTotpSecretPayload);

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

        await _auditLogManager.AddAuditLogAsync(
            category: "Authentication",
            eventName: "MfaRecoveryCodeUsed",
            subjectId: userId.ToString(),
            payload: new JsonObject { ["remaining"] = recoveryCodes.Count(entry => !entry.UsedAt.HasValue) }.ToJsonString()
        );

        return true;
    }

    private async Task<List<MfaRecoveryCodeEntry>> ReadRecoveryCodesAsync(Guid userId)
    {
        var token = await GetUserTokenAsync(userId, RecoveryCodesTokenName);
        if (string.IsNullOrWhiteSpace(token?.Value))
        {
            return [];
        }

        return JsonSerializer.Deserialize(token.Value, IamModJsonSerializerContext.Default.ListMfaRecoveryCodeEntry) ?? [];
    }

    private async Task SaveRecoveryCodesAsync(Guid userId, List<MfaRecoveryCodeEntry> entries)
    {
        await UpsertUserTokenAsync(userId, RecoveryCodesTokenName, JsonSerializer.Serialize(entries, IamModJsonSerializerContext.Default.ListMfaRecoveryCodeEntry));
    }

    private (List<string> plainCodes, List<MfaRecoveryCodeEntry> entries) CreateRecoveryCodes()
    {
        var plainCodes = new List<string>();
        var entries = new List<MfaRecoveryCodeEntry>();

        for (var i = 0; i < 10; i++)
        {
            var plainCode = BuildRecoveryCode();
            plainCodes.Add(plainCode);

            var normalizedCode = MfaTotpService.NormalizeCode(plainCode);
            var salt = HashCrypto.BuildSalt();
            entries.Add(new MfaRecoveryCodeEntry(
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
        return await _dbContext.UserTokens
            .Where(q => q.UserId == userId && q.LoginProvider == LoginProvider && q.Name == name)
            .FirstOrDefaultAsync();
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
            await _dbContext.UserTokens.InsertAsync(token, returning: false);
        }
        else
        {
            await _dbContext.UserTokens
                .Where(item => item.Id == token.Id)
                .ExecuteUpdateAsync(update => update
                    .Set(item => item.Value, value)
                    .Set(item => item.UpdatedTime, DateTime.UtcNow));
        }
    }

    private async Task RemoveUserTokenAsync(Guid userId, string name)
    {
        var token = await GetUserTokenAsync(userId, name);
        if (token == null)
        {
            return;
        }

        await _dbContext.UserTokens
            .Where(item => item.Id == token.Id)
            .ExecuteDeleteAsync();
    }

    private Task PersistUserSecurityStateAsync(Guid userId, bool isTwoFactorEnabled, string? securityStamp, DateTimeOffset updatedTime) =>
        _dbContext.Users
            .Where(item => item.Id == userId)
            .ExecuteUpdateAsync(update => update
                .Set(item => item.IsTwoFactorEnabled, isTwoFactorEnabled)
                .Set(item => item.SecurityStamp, securityStamp)
                .Set(item => item.UpdatedTime, updatedTime));



}
