using System.Security.Cryptography;
using Entity.IAMMod;
using EntityFramework.AppDbContext;
using Share.Constants;

namespace IAMMod.Managers;

/// <summary>
/// Manager for OAuth device flow operations
/// </summary>
public class DeviceFlowManager(DefaultDbContext dbContext, ILogger<DeviceFlowManager> logger) : ManagerBase<DefaultDbContext>(dbContext, logger)
{
    private const int DeviceCodeExpirationSeconds = 600; // 10 minutes
    private const int PollingIntervalSeconds = 5;

    /// <summary>
    /// Initiate device authorization
    /// </summary>
    public async Task<DeviceAuthorizationResponseDto?> InitiateDeviceAuthorizationAsync(
        DeviceAuthorizationRequestDto request
    )
    {
        // Validate client
        var client = await _dbContext.Clients.FirstOrDefaultAsync(c =>
            c.ClientId == request.ClientId
        );

        if (client == null)
        {
            return null;
        }

        // Generate codes
        var deviceCode = GenerateDeviceCode();
        var userCode = GenerateUserCode();

        // Create authorization
        var authorization = new Authorization
        {
            SubjectId = "", // Will be set when user authorizes
            ClientId = client.Id,
            Type = AuthorizationTypes.DeviceCode,
            Status = AuthorizationStatuses.Pending,
            Scopes = request.Scope,
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = DateTimeOffset.UtcNow.AddSeconds(DeviceCodeExpirationSeconds),
            Properties = System.Text.Json.JsonSerializer.Serialize(new { user_code = userCode })
        };

        await _dbContext.Authorizations.AddAsync(authorization);

        // Create device code token
        var deviceCodeToken = new Token
        {
            AuthorizationId = authorization.Id,
            ReferenceId = deviceCode,
            Type = TokenTypes.DeviceCode,
            Status = TokenStatuses.Pending,
            Payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                user_code = userCode,
                client_id = client.ClientId
            }),
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = DateTimeOffset.UtcNow.AddSeconds(DeviceCodeExpirationSeconds)
        };

        // Create user code token for lookup
        var userCodeToken = new Token
        {
            AuthorizationId = authorization.Id,
            ReferenceId = userCode,
            Type = TokenTypes.UserCode,
            Status = TokenStatuses.Pending,
            Payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                device_code = deviceCode,
                client_id = client.ClientId
            }),
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = DateTimeOffset.UtcNow.AddSeconds(DeviceCodeExpirationSeconds)
        };

        await _dbContext.Tokens.AddAsync(deviceCodeToken);
        await _dbContext.Tokens.AddAsync(userCodeToken);
        await _dbContext.SaveChangesAsync();

        // TODO: Get verification URI from configuration
        var verificationUri = "https://localhost:5001/device";

        return new DeviceAuthorizationResponseDto
        {
            DeviceCode = deviceCode,
            UserCode = userCode,
            VerificationUri = verificationUri,
            VerificationUriComplete = $"{verificationUri}?user_code={userCode}",
            ExpiresIn = DeviceCodeExpirationSeconds,
            Interval = PollingIntervalSeconds
        };
    }

    /// <summary>
    /// Get device authorization by user code
    /// </summary>
    public async Task<(Authorization? authorization, Client? client)> GetDeviceAuthorizationByUserCodeAsync(
        string userCode
    )
    {
        var normalizedUserCode = NormalizeUserCode(userCode);

        var token = await _dbContext.Tokens
            .Include(t => t.Authorization)
                .ThenInclude(a => a!.Client)
            .FirstOrDefaultAsync(t => t.ReferenceId == normalizedUserCode && t.Type == TokenTypes.UserCode);

        if (token == null || token.Authorization == null)
        {
            return (null, null);
        }

        // Check expiration
        if (token.ExpirationDate < DateTimeOffset.UtcNow)
        {
            return (null, null);
        }

        return (token.Authorization, token.Authorization.Client);
    }

    /// <summary>
    /// Get detailed device authorization interaction context by user code.
    /// </summary>
    public async Task<DeviceAuthorizationInteractionDto> GetDeviceAuthorizationInteractionAsync(string userCode)
    {
        var normalizedUserCode = NormalizeUserCode(userCode);

        var token = await _dbContext.Tokens
            .Include(t => t.Authorization)
                .ThenInclude(a => a!.Client)
            .FirstOrDefaultAsync(t => t.ReferenceId == normalizedUserCode && t.Type == TokenTypes.UserCode);

        if (token == null || token.Authorization == null)
        {
            return CreateInteractionResult(normalizedUserCode, "invalid", "Invalid or unknown user code.");
        }

        if (token.ExpirationDate < DateTimeOffset.UtcNow)
        {
            return CreateInteractionResult(normalizedUserCode, "expired", "This user code has expired.", expiresAt: token.ExpirationDate);
        }

        var authorization = token.Authorization;
        var client = authorization.Client;
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

    /// <summary>
    /// Approve device authorization
    /// </summary>
    public async Task<bool> ApproveDeviceAuthorizationAsync(string userCode, string userId)
    {
        var normalizedUserCode = NormalizeUserCode(userCode);

        var token = await _dbContext.Tokens
            .Include(t => t.Authorization)
            .FirstOrDefaultAsync(t => t.ReferenceId == normalizedUserCode && t.Type == TokenTypes.UserCode);

        if (token == null || token.Authorization == null)
        {
            return false;
        }

        // Check expiration
        if (token.ExpirationDate < DateTimeOffset.UtcNow)
        {
            return false;
        }

        if (token.Status != TokenStatuses.Pending || token.Authorization.Status != AuthorizationStatuses.Pending)
        {
            return false;
        }

        // Update authorization
        token.Authorization.SubjectId = userId;
        token.Authorization.Status = AuthorizationStatuses.Authorized;

        // Update all related tokens
        var deviceCodeToken = await _dbContext.Tokens.FirstOrDefaultAsync(t =>
            t.AuthorizationId == token.AuthorizationId && t.Type == TokenTypes.DeviceCode
        );

        if (deviceCodeToken != null)
        {
            deviceCodeToken.Status = TokenStatuses.Valid;
            deviceCodeToken.SubjectId = userId;
        }

        token.Status = TokenStatuses.Valid;
        token.SubjectId = userId;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Deny device authorization
    /// </summary>
    public async Task<bool> DenyDeviceAuthorizationAsync(string userCode)
    {
        var normalizedUserCode = NormalizeUserCode(userCode);

        var token = await _dbContext.Tokens
            .Include(t => t.Authorization)
            .FirstOrDefaultAsync(t => t.ReferenceId == normalizedUserCode && t.Type == TokenTypes.UserCode);

        if (token == null || token.Authorization == null)
        {
            return false;
        }

        if (token.ExpirationDate < DateTimeOffset.UtcNow)
        {
            return false;
        }

        if (token.Status != TokenStatuses.Pending || token.Authorization.Status != AuthorizationStatuses.Pending)
        {
            return false;
        }

        // Update authorization
        token.Authorization.Status = AuthorizationStatuses.Denied;

        // Update all related tokens
        var deviceCodeToken = await _dbContext.Tokens.FirstOrDefaultAsync(t =>
            t.AuthorizationId == token.AuthorizationId && t.Type == TokenTypes.DeviceCode
        );

        if (deviceCodeToken != null)
        {
            deviceCodeToken.Status = TokenStatuses.Denied;
        }

        token.Status = TokenStatuses.Denied;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Generate device code
    /// </summary>
    private string GenerateDeviceCode()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>
    /// Generate user code (8 characters, uppercase, alphanumeric)
    /// </summary>
    private string GenerateUserCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Exclude ambiguous characters
        var bytes = new byte[8];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);

        var result = new char[8];
        for (int i = 0; i < 8; i++)
        {
            result[i] = chars[bytes[i] % chars.Length];
        }

        // Format as XXXX-XXXX
        return $"{new string(result, 0, 4)}-{new string(result, 4, 4)}";
    }

    private static string NormalizeUserCode(string userCode)
    {
        return userCode.Trim().ToUpperInvariant();
    }

    private async Task<List<OAuthInteractionScopeDto>> BuildScopeDtosAsync(string? scope)
    {
        var scopeNames = (scope ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (scopeNames.Length == 0)
        {
            return [];
        }

        var scopeMap = await _dbContext.ApiScopes
            .Where(s => scopeNames.Contains(s.Name))
            .ToDictionaryAsync(s => s.Name, StringComparer.OrdinalIgnoreCase);

        return scopeNames.Select(scopeName =>
        {
            if (scopeMap.TryGetValue(scopeName, out var scopeInfo))
            {
                return new OAuthInteractionScopeDto
                {
                    Name = scopeName,
                    DisplayName = scopeInfo.DisplayName ?? scopeName,
                    Description = scopeInfo.Description ?? GetDefaultScopeDescription(scopeName),
                    Required = scopeInfo.Required,
                };
            }

            return new OAuthInteractionScopeDto
            {
                Name = scopeName,
                DisplayName = scopeName,
                Description = GetDefaultScopeDescription(scopeName),
                Required = IsDefaultRequiredScope(scopeName),
            };
        }).ToList();
    }

    private static DeviceAuthorizationInteractionDto CreateInteractionResult(
        string userCode,
        string status,
        string? message,
        DateTimeOffset? expiresAt = null)
    {
        return new DeviceAuthorizationInteractionDto
        {
            UserCode = userCode,
            Status = status,
            Message = message,
            ExpiresAt = expiresAt,
            CanApprove = false,
            CanDeny = false,
        };
    }

    private static string ResolveInteractionStatus(Token token, Authorization authorization)
    {
        if (authorization.Status == AuthorizationStatuses.Denied || token.Status == TokenStatuses.Denied)
        {
            return "denied";
        }

        if (authorization.Status == AuthorizationStatuses.Authorized || token.Status == TokenStatuses.Valid)
        {
            return "approved";
        }

        return "pending";
    }

    private static string GetDefaultScopeDescription(string scopeName)
    {
        return scopeName switch
        {
            Scopes.OpenId => "Your basic identity",
            Scopes.Profile => "Your basic profile details",
            Scopes.Email => "Your email address",
            Scopes.Phone => "Your phone number",
            Scopes.Address => "Your address details",
            "offline_access" => "Access to your data while you are offline",
            _ => $"Access permission for {scopeName}",
        };
    }

    private static bool IsDefaultRequiredScope(string scopeName)
    {
        return scopeName == Scopes.OpenId;
    }
}
