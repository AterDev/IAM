using System.Security.Cryptography;
using System.Text;
using Entity.AccessMod;
using EntityFramework.DBProvider;
using IdentityMod.Models.OAuthDtos;

namespace IdentityMod.Managers;

/// <summary>
/// Manager for OAuth/OIDC authorization operations
/// </summary>
public class AuthorizationManager(DefaultDbContext dbContext, ILogger<AuthorizationManager> logger) : ManagerBase<DefaultDbContext>(dbContext, logger)
{

    /// <summary>
    /// Validate authorization request
    /// </summary>
    public async Task<(bool isValid, string? error, Client? client)> ValidateAuthorizationRequestAsync(
        AuthorizeRequestDto request
    )
    {
        // Validate client
        var client = await _dbContext.Clients
            .Include(c => c.ClientScopes)
                .ThenInclude(cs => cs.Scope)
            .FirstOrDefaultAsync(c => c.ClientId == request.ClientId);

        if (client == null)
        {
            return (false, OAuthConstants.ErrorCodes.InvalidClient, null);
        }

        // Validate redirect URI
        if (!client.RedirectUris.Contains(request.RedirectUri))
        {
            return (false, OAuthConstants.ErrorCodes.InvalidRequest, client);
        }

        // Validate response type
        var supportedResponseTypes = new[] { OAuthConstants.ResponseTypes.Code, OAuthConstants.ResponseTypes.Token, OAuthConstants.ResponseTypes.IdToken };
        if (string.IsNullOrEmpty(request.ResponseType) ||
            !supportedResponseTypes.Contains(request.ResponseType.Split(' ')[0]))
        {
            return (false, OAuthConstants.ErrorCodes.UnsupportedResponseType, client);
        }

        // Validate PKCE if required
        if (client.RequirePkce)
        {
            if (string.IsNullOrEmpty(request.CodeChallenge))
            {
                return (false, OAuthConstants.ErrorCodes.InvalidRequest, client);
            }

            var supportedMethods = new[] { OAuthConstants.CodeChallengeMethods.Plain, OAuthConstants.CodeChallengeMethods.S256 };
            if (!string.IsNullOrEmpty(request.CodeChallengeMethod) &&
                !supportedMethods.Contains(request.CodeChallengeMethod))
            {
                return (false, OAuthConstants.ErrorCodes.InvalidRequest, client);
            }
        }

        // Validate scopes
        if (!string.IsNullOrEmpty(request.Scope))
        {
            var requestedScopes = request.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var clientScopeNames = client.ClientScopes.Select(cs => cs.Scope.Name).ToList();

            foreach (var scope in requestedScopes)
            {
                if (!clientScopeNames.Contains(scope) && scope != OAuthConstants.Scopes.OpenId && scope != OAuthConstants.Scopes.Profile)
                {
                    return (false, OAuthConstants.ErrorCodes.InvalidScope, client);
                }
            }
        }

        return (true, null, client);
    }

    /// <summary>
    /// Create authorization code
    /// </summary>
    public async Task<string> CreateAuthorizationCodeAsync(
        string subjectId,
        Guid clientId,
        string redirectUri,
        string? scope,
        string? codeChallenge,
        string? codeChallengeMethod,
        string? nonce
    )
    {
        var code = GenerateAuthorizationCode();

        // Create authorization record
        var authorization = new Authorization
        {
            SubjectId = subjectId,
            ClientId = clientId,
            Type = OAuthConstants.AuthorizationTypes.Code,
            Status = OAuthConstants.AuthorizationStatuses.Valid,
            Scopes = scope,
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = DateTimeOffset.UtcNow.AddMinutes(10),
            Properties = System.Text.Json.JsonSerializer.Serialize(new
            {
                redirect_uri = redirectUri,
                code_challenge = codeChallenge,
                code_challenge_method = codeChallengeMethod,
                nonce
            })
        };

        await _dbContext.Authorizations.AddAsync(authorization);

        // Create token record for the code
        var token = new Token
        {
            AuthorizationId = authorization.Id,
            ReferenceId = code,
            Type = OAuthConstants.TokenTypes.AuthorizationCode,
            Status = OAuthConstants.TokenStatuses.Valid,
            SubjectId = subjectId,
            Payload = System.Text.Json.JsonSerializer.Serialize(new { authorization_id = authorization.Id }),
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = DateTimeOffset.UtcNow.AddMinutes(10)
        };

        await _dbContext.Tokens.AddAsync(token);
        await _dbContext.SaveChangesAsync();

        return code;
    }

    /// <summary>
    /// Validate authorization code and PKCE
    /// </summary>
    public async Task<(bool isValid, Authorization? authorization)> ValidateAuthorizationCodeAsync(
        string code,
        string clientId,
        string redirectUri,
        string? codeVerifier
    )
    {
        var token = await _dbContext.Tokens
            .Include(t => t.Authorization)
                .ThenInclude(a => a!.Client)
            .FirstOrDefaultAsync(t =>
                t.ReferenceId == code &&
                t.Type == OAuthConstants.TokenTypes.AuthorizationCode &&
                t.Status == OAuthConstants.TokenStatuses.Valid
            );

        if (token == null || token.Authorization == null)
        {
            return (false, null);
        }

        // Check expiration
        if (token.ExpirationDate < DateTimeOffset.UtcNow)
        {
            return (false, null);
        }

        // Validate client
        if (token.Authorization.Client.ClientId != clientId)
        {
            return (false, null);
        }

        // Validate redirect URI
        var properties = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(
            token.Authorization.Properties ?? "{}"
        );
        if (properties?.GetValueOrDefault("redirect_uri") != redirectUri)
        {
            return (false, null);
        }

        // Validate PKCE if present
        var codeChallenge = properties?.GetValueOrDefault("code_challenge");
        var codeChallengeMethod = properties?.GetValueOrDefault("code_challenge_method");

        if (!string.IsNullOrEmpty(codeChallenge))
        {
            if (string.IsNullOrEmpty(codeVerifier))
            {
                return (false, null);
            }

            var isValidPkce = ValidatePkce(codeVerifier, codeChallenge, codeChallengeMethod ?? OAuthConstants.CodeChallengeMethods.Plain);
            if (!isValidPkce)
            {
                return (false, null);
            }
        }

        // Mark code as redeemed
        token.Status = OAuthConstants.TokenStatuses.Redeemed;
        token.RedemptionDate = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        return (true, token.Authorization);
    }

    /// <summary>
    /// Validate PKCE challenge
    /// </summary>
    private bool ValidatePkce(string verifier, string challenge, string method)
    {
        if (method == OAuthConstants.CodeChallengeMethods.Plain)
        {
            return verifier == challenge;
        }
        else if (method == OAuthConstants.CodeChallengeMethods.S256)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(verifier));
            var computed = Convert.ToBase64String(hash)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
            return computed == challenge;
        }

        return false;
    }

    /// <summary>
    /// Generate authorization code
    /// </summary>
    private string GenerateAuthorizationCode()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
