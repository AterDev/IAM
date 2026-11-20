using System.Security.Cryptography;
using Entity.IAMMod;
using EntityFramework.AppDbFactory;
using IAMMod.Serialization;
using Perigon.PostgreSQL;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Share.Constants;

namespace IAMMod.Managers;

/// <summary>Manager for OAuth device flow operations.</summary>
public class DeviceFlowManager(
    AppDbFactory dbContextFactory,
    IUserContext userContext,
    ILogger<DeviceFlowManager> logger
) : ManagerBase<DefaultDbContext, Authorization>(dbContextFactory, userContext, logger)
{
    private const int DeviceCodeExpirationSeconds = 600;
    private const int PollingIntervalSeconds = 5;

    public override Task<bool> HasPermissionAsync(Guid id) => Task.FromResult(_userContext.IsAdmin);

    /// <summary>Initiate device authorization.</summary>
    public async Task<DeviceAuthorizationResponseDto?> InitiateDeviceAuthorizationAsync(
        DeviceAuthorizationRequestDto request)
    {
        var client = await _dbContext.Clients
            .Where(item => item.ClientIdentifier == request.ClientId)
            .FirstOrDefaultAsync();
        if (client is null)
        {
            return null;
        }

        var deviceCode = GenerateDeviceCode();
        var userCode = GenerateUserCode();
        var now = DateTimeOffset.UtcNow;
        var expiration = now.AddSeconds(DeviceCodeExpirationSeconds);
        var authorization = new Authorization
        {
            SubjectId = string.Empty,
            ClientId = client.Id,
            Type = AuthorizationTypes.DeviceCode,
            Status = AuthorizationStatuses.Pending,
            Scopes = request.Scope,
            CreationDate = now,
            ExpirationDate = expiration,
            Properties = System.Text.Json.JsonSerializer.Serialize(
                new Dictionary<string, string> { [TokenTypes.UserCode] = userCode },
                IamModJsonSerializerContext.Default.DictionaryStringString),
        };
        var deviceCodeToken = new Token
        {
            AuthorizationId = authorization.Id,
            ReferenceId = deviceCode,
            Type = TokenTypes.DeviceCode,
            Status = TokenStatuses.Pending,
            Payload = System.Text.Json.JsonSerializer.Serialize(
                new Dictionary<string, string>
                {
                    [TokenTypes.UserCode] = userCode,
                    [OpenIdConnectParameterNames.ClientId] = client.ClientId,
                },
                IamModJsonSerializerContext.Default.DictionaryStringString),
            CreationDate = now,
            ExpirationDate = expiration,
        };
        var userCodeToken = new Token
        {
            AuthorizationId = authorization.Id,
            ReferenceId = userCode,
            Type = TokenTypes.UserCode,
            Status = TokenStatuses.Pending,
            Payload = System.Text.Json.JsonSerializer.Serialize(
                new Dictionary<string, string>
                {
                    [TokenTypes.DeviceCode] = deviceCode,
                    [OpenIdConnectParameterNames.ClientId] = client.ClientId,
                },
                IamModJsonSerializerContext.Default.DictionaryStringString),
            CreationDate = now,
            ExpirationDate = expiration,
        };

        await _dbContext.TransactionAsync(async ct =>
        {
            await _dbContext.Authorizations.InsertAsync(authorization, ct);
            await _dbContext.Tokens.InsertAsync(deviceCodeToken, ct);
            await _dbContext.Tokens.InsertAsync(userCodeToken, ct);
        });

        const string verificationUri = "https://localhost:5001/device";
        return new DeviceAuthorizationResponseDto
        {
            DeviceCode = deviceCode,
            UserCode = userCode,
            VerificationUri = verificationUri,
            VerificationUriComplete = $"{verificationUri}?user_code={userCode}",
            ExpiresIn = DeviceCodeExpirationSeconds,
            Interval = PollingIntervalSeconds,
        };
    }

    /// <summary>Get device authorization by user code.</summary>
    public async Task<(Authorization? authorization, Client? client)> GetDeviceAuthorizationByUserCodeAsync(
        string userCode)
    {
        var token = await FindUserCodeTokenAsync(userCode);
        if (token is null || token.AuthorizationId is not Guid authorizationId
            || token.ExpirationDate < DateTimeOffset.UtcNow)
        {
            return (null, null);
        }
        var authorization = await _dbContext.Authorizations
            .Where(item => item.Id == authorizationId)
            .FirstOrDefaultAsync();
        if (authorization is null)
        {
            return (null, null);
        }
        var client = await _dbContext.Clients
            .Where(item => item.Id == authorization.ClientId)
            .FirstOrDefaultAsync();
        authorization.Client = client!;
        return (authorization, client);
    }

    /// <summary>Get detailed device authorization interaction context.</summary>
    public async Task<DeviceAuthorizationInteractionDto> GetDeviceAuthorizationInteractionAsync(string userCode)
    {
        var normalizedUserCode = NormalizeUserCode(userCode);
        var token = await FindUserCodeTokenAsync(normalizedUserCode);
        if (token is null || token.AuthorizationId is not Guid authorizationId)
        {
            return CreateInteractionResult(normalizedUserCode, "invalid", "Invalid or unknown user code.");
        }
        if (token.ExpirationDate < DateTimeOffset.UtcNow)
        {
            return CreateInteractionResult(normalizedUserCode, "expired", "This user code has expired.", token.ExpirationDate);
        }

        var authorization = await _dbContext.Authorizations
            .Where(item => item.Id == authorizationId)
            .FirstOrDefaultAsync();
        if (authorization is null)
        {
            return CreateInteractionResult(normalizedUserCode, "invalid", "Invalid or unknown user code.");
        }
        var client = await _dbContext.Clients
            .Where(item => item.Id == authorization.ClientId)
            .FirstOrDefaultAsync();
        var requestedScopes = await BuildScopeDtosAsync(authorization.Scopes);
        var status = ResolveInteractionStatus(token, authorization);
        return new DeviceAuthorizationInteractionDto
        {
            UserCode = normalizedUserCode,
            Status = status,
            Message = status switch
            {
                "approved" => "Device authorization approved.",
                "denied" => "Device authorization was denied.",
                _ => null,
            },
            ClientId = client?.ClientId,
            ClientName = client?.DisplayName ?? client?.ClientId,
            ClientDescription = client?.Description,
            Scope = authorization.Scopes,
            RequestedScopes = requestedScopes,
            ExpiresAt = token.ExpirationDate,
            CanApprove = status == "pending",
            CanDeny = status == "pending",
        };
    }

    /// <summary>Approve a pending device authorization.</summary>
    public async Task<bool> ApproveDeviceAuthorizationAsync(string userCode, string userId)
    {
        var token = await FindUserCodeTokenAsync(userCode);
        if (token is null || token.AuthorizationId is not Guid authorizationId
            || token.ExpirationDate < DateTimeOffset.UtcNow)
        {
            return false;
        }
        var authorization = await _dbContext.Authorizations
            .Where(item => item.Id == authorizationId)
            .FirstOrDefaultAsync();
        if (authorization is null
            || token.Status != TokenStatuses.Pending
            || authorization.Status != AuthorizationStatuses.Pending)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        var updated = false;
        await _dbContext.TransactionAsync(async ct =>
        {
            var authCount = await _dbContext.Authorizations
                .Where(item => item.Id == authorizationId && item.Status == AuthorizationStatuses.Pending)
                .ExecuteUpdateAsync(update => update
                    .Set(item => item.SubjectId, userId)
                    .Set(item => item.Status, AuthorizationStatuses.Authorized)
                    .Set(item => item.UpdatedTime, now), cancellationToken: ct);
            if (authCount == 0)
            {
                return;
            }

            await _dbContext.Tokens
                .Where(item => item.AuthorizationId == authorizationId
                    && item.Type == TokenTypes.DeviceCode
                    && item.Status == TokenStatuses.Pending)
                .ExecuteUpdateAsync(update => update
                    .Set(item => item.Status, TokenStatuses.Valid)
                    .Set(item => item.SubjectId, userId)
                    .Set(item => item.UpdatedTime, now), cancellationToken: ct);
            await _dbContext.Tokens
                .Where(item => item.Id == token.Id && item.Status == TokenStatuses.Pending)
                .ExecuteUpdateAsync(update => update
                    .Set(item => item.Status, TokenStatuses.Valid)
                    .Set(item => item.SubjectId, userId)
                    .Set(item => item.UpdatedTime, now), cancellationToken: ct);
            updated = true;
        });
        return updated;
    }

    /// <summary>Deny a pending device authorization.</summary>
    public async Task<bool> DenyDeviceAuthorizationAsync(string userCode)
    {
        var token = await FindUserCodeTokenAsync(userCode);
        if (token is null || token.AuthorizationId is not Guid authorizationId
            || token.ExpirationDate < DateTimeOffset.UtcNow)
        {
            return false;
        }
        var authorization = await _dbContext.Authorizations
            .Where(item => item.Id == authorizationId)
            .FirstOrDefaultAsync();
        if (authorization is null
            || token.Status != TokenStatuses.Pending
            || authorization.Status != AuthorizationStatuses.Pending)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        var updated = false;
        await _dbContext.TransactionAsync(async ct =>
        {
            var authCount = await _dbContext.Authorizations
                .Where(item => item.Id == authorizationId && item.Status == AuthorizationStatuses.Pending)
                .ExecuteUpdateAsync(update => update
                    .Set(item => item.Status, AuthorizationStatuses.Denied)
                    .Set(item => item.UpdatedTime, now), cancellationToken: ct);
            if (authCount == 0)
            {
                return;
            }

            await _dbContext.Tokens
                .Where(item => item.AuthorizationId == authorizationId
                    && item.Type == TokenTypes.DeviceCode
                    && item.Status == TokenStatuses.Pending)
                .ExecuteUpdateAsync(update => update
                    .Set(item => item.Status, TokenStatuses.Denied)
                    .Set(item => item.UpdatedTime, now), cancellationToken: ct);
            await _dbContext.Tokens
                .Where(item => item.Id == token.Id && item.Status == TokenStatuses.Pending)
                .ExecuteUpdateAsync(update => update
                    .Set(item => item.Status, TokenStatuses.Denied)
                    .Set(item => item.UpdatedTime, now), cancellationToken: ct);
            updated = true;
        });
        return updated;
    }

    private async Task<Token?> FindUserCodeTokenAsync(string userCode)
    {
        // Normalize before composing the provider query. Perigon's SQL translator
        // intentionally does not translate arbitrary helper method calls.
        var normalizedUserCode = NormalizeUserCode(userCode);
        return await _dbContext.Tokens
            .Where(item => item.ReferenceId == normalizedUserCode && item.Type == TokenTypes.UserCode)
            .FirstOrDefaultAsync();
    }

    private static string GenerateDeviceCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string GenerateUserCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = RandomNumberGenerator.GetBytes(8);
        var result = new char[8];
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = chars[bytes[i] % chars.Length];
        }
        return $"{new string(result, 0, 4)}-{new string(result, 4, 4)}";
    }

    private static string NormalizeUserCode(string userCode) => userCode.Trim().ToUpperInvariant();

    private async Task<List<OAuthInteractionScopeDto>> BuildScopeDtosAsync(string? scope)
    {
        var scopeNames = (scope ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (scopeNames.Length == 0)
        {
            return [];
        }

        var scopeRows = await _dbContext.ApiScopes
            .Where(item => Enumerable.Contains(scopeNames, item.Name))
            .ToListAsync();
        var scopeMap = scopeRows.ToDictionary(item => item.Name, StringComparer.OrdinalIgnoreCase);
        return scopeNames.Select(scopeName => scopeMap.TryGetValue(scopeName, out var scopeInfo)
            ? new OAuthInteractionScopeDto
            {
                Name = scopeName,
                DisplayName = scopeInfo.DisplayName ?? scopeName,
                Description = scopeInfo.Description ?? GetDefaultScopeDescription(scopeName),
                Required = scopeInfo.Required,
            }
            : new OAuthInteractionScopeDto
            {
                Name = scopeName,
                DisplayName = scopeName,
                Description = GetDefaultScopeDescription(scopeName),
                Required = IsDefaultRequiredScope(scopeName),
            }).ToList();
    }

    private static DeviceAuthorizationInteractionDto CreateInteractionResult(
        string userCode,
        string status,
        string? message,
        DateTimeOffset? expiresAt = null) => new()
        {
            UserCode = userCode,
            Status = status,
            Message = message,
            ExpiresAt = expiresAt,
            CanApprove = false,
            CanDeny = false,
        };

    private static string ResolveInteractionStatus(Token token, Authorization authorization) =>
        authorization.Status == AuthorizationStatuses.Denied || token.Status == TokenStatuses.Denied
            ? "denied"
            : authorization.Status == AuthorizationStatuses.Authorized || token.Status == TokenStatuses.Valid
                ? "approved"
                : "pending";

    private static string GetDefaultScopeDescription(string scopeName) => scopeName switch
    {
        OpenIdConnectScope.OpenId => "Your basic identity",
        Scopes.Profile => "Your basic profile details",
        OpenIdConnectScope.Email => "Your email address",
        OpenIdConnectScope.Phone => "Your phone number",
        OpenIdConnectScope.Address => "Your address details",
        OpenIdConnectScope.OfflineAccess => "Access to your data while you are offline",
        _ => $"Access permission for {scopeName}",
    };

    private static bool IsDefaultRequiredScope(string scopeName) => scopeName == OpenIdConnectScope.OpenId;
}
