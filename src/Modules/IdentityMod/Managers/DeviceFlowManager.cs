using System.Security.Cryptography;
using Entity.AccessMod;
using EntityFramework.DBProvider;
using IdentityMod.Models.OAuthDtos;

namespace IdentityMod.Managers;

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
            Type = "device_code",
            Status = "pending",
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
            Type = "device_code",
            Status = "pending",
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
            Type = "user_code",
            Status = "pending",
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
        var token = await _dbContext.Tokens
            .Include(t => t.Authorization)
                .ThenInclude(a => a!.Client)
            .FirstOrDefaultAsync(t => t.ReferenceId == userCode && t.Type == "user_code");

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
    /// Approve device authorization
    /// </summary>
    public async Task<bool> ApproveDeviceAuthorizationAsync(string userCode, string userId)
    {
        var token = await _dbContext.Tokens
            .Include(t => t.Authorization)
            .FirstOrDefaultAsync(t => t.ReferenceId == userCode && t.Type == "user_code");

        if (token == null || token.Authorization == null)
        {
            return false;
        }

        // Check expiration
        if (token.ExpirationDate < DateTimeOffset.UtcNow)
        {
            return false;
        }

        // Update authorization
        token.Authorization.SubjectId = userId;
        token.Authorization.Status = "authorized";

        // Update all related tokens
        var deviceCodeToken = await _dbContext.Tokens.FirstOrDefaultAsync(t =>
            t.AuthorizationId == token.AuthorizationId && t.Type == "device_code"
        );

        if (deviceCodeToken != null)
        {
            deviceCodeToken.Status = "valid";
            deviceCodeToken.SubjectId = userId;
        }

        token.Status = "valid";
        token.SubjectId = userId;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Deny device authorization
    /// </summary>
    public async Task<bool> DenyDeviceAuthorizationAsync(string userCode)
    {
        var token = await _dbContext.Tokens
            .Include(t => t.Authorization)
            .FirstOrDefaultAsync(t => t.ReferenceId == userCode && t.Type == "user_code");

        if (token == null || token.Authorization == null)
        {
            return false;
        }

        // Update authorization
        token.Authorization.Status = "denied";

        // Update all related tokens
        var deviceCodeToken = await _dbContext.Tokens.FirstOrDefaultAsync(t =>
            t.AuthorizationId == token.AuthorizationId && t.Type == "device_code"
        );

        if (deviceCodeToken != null)
        {
            deviceCodeToken.Status = "denied";
        }

        token.Status = "denied";

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
}
