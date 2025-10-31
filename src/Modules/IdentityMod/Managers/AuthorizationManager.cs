using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IdentityMod.Models.OAuthDtos;
using IdentityMod.Services;


namespace IdentityMod.Managers;

/// <summary>
/// Manager for OAuth/OIDC authorization operations
/// </summary>
public class AuthorizationManager(DefaultDbContext dbContext, ILogger<AuthorizationManager> logger, OAuthService oAuthService) : ManagerBase<DefaultDbContext>(dbContext, logger)
{
    private readonly OAuthService _oAuthService = oAuthService;

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
            return (false, ErrorCodes.InvalidClient, null);
        }

        // Validate redirect URI
        if (!client.RedirectUris.Contains(request.RedirectUri))
        {
            return (false, ErrorCodes.InvalidRequest, client);
        }

        // Validate response type
        var supportedResponseTypes = new[] { ResponseTypes.Code, ResponseTypes.Token, ResponseTypes.IdToken };
        if (string.IsNullOrEmpty(request.ResponseType) ||
            !supportedResponseTypes.Contains(request.ResponseType.Split(' ')[0]))
        {
            return (false, ErrorCodes.UnsupportedResponseType, client);
        }

        // Validate PKCE if required
        if (client.RequirePkce)
        {
            if (string.IsNullOrEmpty(request.CodeChallenge))
            {
                return (false, ErrorCodes.InvalidRequest, client);
            }

            var supportedMethods = new[] { CodeChallengeMethods.Plain, CodeChallengeMethods.S256 };
            if (!string.IsNullOrEmpty(request.CodeChallengeMethod) &&
                !supportedMethods.Contains(request.CodeChallengeMethod))
            {
                return (false, ErrorCodes.InvalidRequest, client);
            }
        }

        // Validate scopes
        if (!string.IsNullOrEmpty(request.Scope))
        {
            var requestedScopes = request.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var clientScopeNames = client.ClientScopes.Select(cs => cs.Scope.Name).ToList();

            foreach (var scope in requestedScopes)
            {
                if (!clientScopeNames.Contains(scope) && scope != Scopes.OpenId && scope != Scopes.Profile)
                {
                    return (false, ErrorCodes.InvalidScope, client);
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
        var code = _oAuthService.GenerateAuthorizationCode();

        // Create authorization record
        var authorization = new Authorization
        {
            SubjectId = subjectId,
            ClientId = clientId,
            Type = AuthorizationTypes.Code,
            Status = AuthorizationStatuses.Valid,
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
            Type = TokenTypes.AuthorizationCode,
            Status = TokenStatuses.Valid,
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
                t.Type == TokenTypes.AuthorizationCode &&
                t.Status == TokenStatuses.Valid
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

            var isValidPkce = _oAuthService.ValidatePkce(codeVerifier, codeChallenge, codeChallengeMethod ?? CodeChallengeMethods.Plain);
            if (!isValidPkce)
            {
                return (false, null);
            }
        }

        // Mark code as redeemed
        token.Status = TokenStatuses.Redeemed;
        token.RedemptionDate = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        return (true, token.Authorization);
    }
}
