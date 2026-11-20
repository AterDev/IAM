using Entity.IAMMod;
using EntityFramework.AppDbFactory;
using IAMMod.Serialization;
using IAMMod.Services;
using Perigon.PostgreSQL;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace IAMMod.Managers;

/// <summary>Manager for OAuth/OIDC authorization operations.</summary>
public class AuthorizationManager(
    AppDbFactory dbContextFactory,
    IUserContext userContext,
    ILogger<AuthorizationManager> logger
) : ManagerBase<DefaultDbContext, Authorization>(dbContextFactory, userContext, logger)
{
    public override Task<bool> HasPermissionAsync(Guid id) => Task.FromResult(_userContext.IsAdmin);

    /// <summary>Validate authorization request.</summary>
    public async Task<(bool isValid, string? error, Client? client)> ValidateAuthorizationRequestAsync(
        AuthorizeRequestDto request)
    {
        var client = await _dbContext.Clients
            .Where(c => c.ClientIdentifier == request.ClientId)
            .FirstOrDefaultAsync();
        if (client == null)
        {
            return (false, ErrorCodes.InvalidClient, null);
        }

        var clientScopes = await _dbContext.ClientScopes
            .Where(scope => scope.ClientId == client.Id)
            .ToListAsync();
        var scopeIds = clientScopes.Select(scope => scope.ScopeId).Distinct().ToArray();
        var scopes = scopeIds.Length == 0
            ? []
            : await _dbContext.ApiScopes.Where(scope => Enumerable.Contains(scopeIds, scope.Id)).ToListAsync();
        var scopesById = scopes.ToDictionary(scope => scope.Id);
        foreach (var clientScope in clientScopes)
        {
            if (scopesById.TryGetValue(clientScope.ScopeId, out var scope))
            {
                clientScope.Scope = scope;
            }
        }
        client.ClientScopes = clientScopes;

        if (!client.RedirectUris.Contains(request.RedirectUri))
        {
            return (false, ErrorCodes.InvalidRequest, client);
        }
        if (!string.Equals(request.ResponseType, OpenIdConnectResponseType.Code, StringComparison.Ordinal))
        {
            return (false, ErrorCodes.UnsupportedResponseType, client);
        }
        if (!string.IsNullOrEmpty(request.ResponseMode)
            && !string.Equals(request.ResponseMode, ResponseModes.Query, StringComparison.Ordinal))
        {
            return (false, ErrorCodes.InvalidRequest, client);
        }

        if (client.RequirePkce)
        {
            if (string.IsNullOrEmpty(request.CodeChallenge))
            {
                return (false, ErrorCodes.InvalidRequest, client);
            }
            var supportedMethods = new[] { CodeChallengeMethods.Plain, CodeChallengeMethods.S256 };
            if (!string.IsNullOrEmpty(request.CodeChallengeMethod)
                && !supportedMethods.Contains(request.CodeChallengeMethod))
            {
                return (false, ErrorCodes.InvalidRequest, client);
            }
        }

        if (!string.IsNullOrEmpty(request.Scope))
        {
            var requestedScopes = request.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var clientScopeNames = client.ClientScopes
                .Where(scope => scope.Scope is not null)
                .Select(scope => scope.Scope.Name)
                .ToHashSet(StringComparer.Ordinal);
            foreach (var scope in requestedScopes)
            {
                if (!clientScopeNames.Contains(scope))
                {
                    return (false, ErrorCodes.InvalidScope, client);
                }
            }
        }

        return (true, null, client);
    }

    /// <summary>Create an authorization code and its one-time token atomically.</summary>
    public async Task<string> CreateAuthorizationCodeAsync(
        string subjectId,
        Guid clientId,
        string redirectUri,
        string? scope,
        string? codeChallenge,
        string? codeChallengeMethod,
        string? nonce,
        string? sessionId = null)
    {
        var code = OAuthService.GenerateAuthorizationCode();
        var now = DateTimeOffset.UtcNow;
        var expiration = now.AddMinutes(10);
        var authorization = new Authorization
        {
            SubjectId = subjectId,
            ClientId = clientId,
            Type = AuthorizationTypes.Code,
            Status = AuthorizationStatuses.Valid,
            Scopes = scope,
            CreationDate = now,
            ExpirationDate = expiration,
            Properties = System.Text.Json.JsonSerializer.Serialize(
                new AuthorizationCodeProperties(redirectUri, codeChallenge, codeChallengeMethod, nonce, sessionId),
                IamModJsonSerializerContext.Default.AuthorizationCodeProperties),
        };
        var token = new Token
        {
            AuthorizationId = authorization.Id,
            ReferenceId = code,
            Type = TokenTypes.AuthorizationCode,
            Status = TokenStatuses.Valid,
            SubjectId = subjectId,
            Payload = System.Text.Json.JsonSerializer.Serialize(
                new AuthorizationCodeTokenPayload(authorization.Id),
                IamModJsonSerializerContext.Default.AuthorizationCodeTokenPayload),
            CreationDate = now,
            ExpirationDate = expiration,
        };

        await _dbContext.TransactionAsync(async ct =>
        {
            await _dbContext.Authorizations.InsertAsync(authorization, ct);
            await _dbContext.Tokens.InsertAsync(token, ct);
        });
        return code;
    }

    /// <summary>Validate and atomically redeem an authorization code.</summary>
    public async Task<(bool isValid, Authorization? authorization)> ValidateAuthorizationCodeAsync(
        string code,
        string clientId,
        string redirectUri,
        string? codeVerifier)
    {
        var token = await _dbContext.Tokens
            .Where(item => item.ReferenceId == code
                && item.Type == TokenTypes.AuthorizationCode
                && item.Status == TokenStatuses.Valid)
            .FirstOrDefaultAsync();
        if (token?.AuthorizationId is not Guid authorizationId)
        {
            return (false, null);
        }

        var authorization = await _dbContext.Authorizations
            .Where(item => item.Id == authorizationId)
            .FirstOrDefaultAsync();
        if (authorization is null)
        {
            return (false, null);
        }
        var client = await _dbContext.Clients
            .Where(item => item.Id == authorization.ClientId)
            .FirstOrDefaultAsync();
        if (client is null)
        {
            return (false, null);
        }
        if (token.ExpirationDate < DateTimeOffset.UtcNow
            || !string.Equals(client.ClientId, clientId, StringComparison.Ordinal))
        {
            return (false, null);
        }

        var properties = string.IsNullOrWhiteSpace(authorization.Properties)
            ? null
            : System.Text.Json.JsonSerializer.Deserialize(
                authorization.Properties,
                IamModJsonSerializerContext.Default.AuthorizationCodeProperties);
        if (properties is null || !string.Equals(properties.RedirectUri, redirectUri, StringComparison.Ordinal))
        {
            return (false, null);
        }
        if (!string.IsNullOrEmpty(properties.CodeChallenge)
            && (string.IsNullOrEmpty(codeVerifier)
                || !OAuthService.ValidatePkce(
                    codeVerifier,
                    properties.CodeChallenge,
                    properties.CodeChallengeMethod ?? CodeChallengeMethods.Plain)))
        {
            return (false, null);
        }

        var redeemedAt = DateTimeOffset.UtcNow;
        var redeemed = await _dbContext.Tokens
            .Where(item => item.Id == token.Id && item.Status == TokenStatuses.Valid)
            .ExecuteUpdateAsync(update => update
                .Set(item => item.Status, TokenStatuses.Redeemed)
                .Set(item => item.RedemptionDate, redeemedAt)
                .Set(item => item.UpdatedTime, redeemedAt));
        if (redeemed == 0)
        {
            return (false, null);
        }

        authorization.Client = client;
        return (true, authorization);
    }
}
