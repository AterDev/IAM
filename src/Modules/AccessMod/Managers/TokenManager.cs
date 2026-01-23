using Entity.IdentityMod;
using Share;
using Share.Constants;
using Share.Exceptions;
using System.Security.Claims;

namespace AccessMod.Managers;

/// <summary>
/// Manager for OAuth/OIDC token operations
/// </summary>
public class TokenManager(
    DefaultDbContext dbContext,
    ILogger<TokenManager> logger,
    OAuthService oauthService,
    IPasswordHasher passwordHasher
) : ManagerBase<DefaultDbContext>(dbContext, logger)
{
    private readonly OAuthService _oauthService = oauthService;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;



    /// <summary>
    /// Process token request with provided signing key
    /// </summary>
    public async Task<TokenResponseDto> ProcessTokenRequestAsync(
        TokenRequestDto request,
        SigningKey signingKey
    )
    {
        return request.GrantType switch
        {
            GrantTypes.AuthorizationCode => await ProcessAuthorizationCodeGrantAsync(request, signingKey),
            GrantTypes.RefreshToken => await ProcessRefreshTokenGrantAsync(request, signingKey),
            GrantTypes.ClientCredentials => await ProcessClientCredentialsGrantAsync(request, signingKey),
            GrantTypes.Password => await ProcessPasswordGrantAsync(request, signingKey),
            GrantTypes.DeviceCode => await ProcessDeviceCodeGrantAsync(request, signingKey),
            _ => throw new BusinessException(Localizer.OAuthUnsupportedGrantType),
        };
    }

    /// <summary>
    /// Process authorization code grant
    /// </summary>
    private async Task<TokenResponseDto> ProcessAuthorizationCodeGrantAsync(
        TokenRequestDto request,
        SigningKey signingKey
    )
    {
        if (string.IsNullOrEmpty(request.Code) || string.IsNullOrEmpty(request.ClientId))
        {
            throw new BusinessException(Localizer.OAuthMissingParameters);
        }

        // Validate client
        var client = await GetValidatedClientAsync(
            request.ClientId,
            request.ClientSecret,
            missingDescription: "Missing required parameters"
        );

        // Validate authorization code
        var token = await _dbContext
            .Tokens.Include(t => t.Authorization)
            .ThenInclude(a => a!.Client)
            .FirstOrDefaultAsync(t =>
                t.ReferenceId == request.Code
                && t.Type == TokenTypes.AuthorizationCode
                && t.Status == TokenStatuses.Valid
            );

        if (token == null || token.Authorization == null)
        {
            throw new BusinessException(Localizer.OAuthInvalidAuthorizationCode);
        }

        // Check expiration
        if (token.ExpirationDate < DateTimeOffset.UtcNow)
        {
            throw new BusinessException(Localizer.OAuthAuthorizationCodeExpired);
        }

        // Validate client
        if (token.Authorization.Client.ClientId != request.ClientId)
        {
            throw new BusinessException(Localizer.OAuthClientMismatch);
        }

        // Validate redirect URI
        var properties = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(
            token.Authorization.Properties ?? "{}"
        );
        if (properties?.GetValueOrDefault("redirect_uri") != request.RedirectUri)
        {
            throw new BusinessException(Localizer.OAuthInvalidRedirectUri);
        }

        // Validate PKCE if present
        var codeChallenge = properties?.GetValueOrDefault("code_challenge");
        var codeChallengeMethod = properties?.GetValueOrDefault("code_challenge_method");

        if (!string.IsNullOrEmpty(codeChallenge))
        {
            if (string.IsNullOrEmpty(request.CodeVerifier))
            {
                throw new BusinessException(Localizer.OAuthMissingCodeVerifier);
            }

            var isValidPkce = OAuthService.ValidatePkce(
                request.CodeVerifier,
                codeChallenge,
                codeChallengeMethod ?? CodeChallengeMethods.Plain
            );
            if (!isValidPkce)
            {
                throw new BusinessException(Localizer.OAuthInvalidCodeVerifier);
            }
        }

        // Mark code as redeemed
        token.Status = TokenStatuses.Redeemed;
        token.RedemptionDate = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        var authorization = token.Authorization;

        // Get user
        var user = await _dbContext.Users.FirstOrDefaultAsync(u =>
            u.Id.ToString() == authorization.SubjectId
        );
        if (user == null)
        {
            throw new BusinessException(Localizer.UserNotFound);
        }

        // Generate tokens
        return await GenerateTokensAsync(user, client, authorization.Scopes, signingKey, authorization.Id);
    }

    /// <summary>
    /// Process refresh token grant
    /// </summary>
    private async Task<TokenResponseDto> ProcessRefreshTokenGrantAsync(
        TokenRequestDto request,
        SigningKey signingKey
    )
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            throw new BusinessException(Localizer.OAuthMissingRefreshToken);
        }

        // Find refresh token
        var tokenEntity = await _dbContext
            .Tokens.Include(t => t.Authorization)
            .ThenInclude(a => a!.Client)
            .FirstOrDefaultAsync(t =>
                t.ReferenceId == request.RefreshToken
                && t.Type == TokenTypes.RefreshToken
                && t.Status == TokenStatuses.Valid
            );

        if (tokenEntity == null || tokenEntity.Authorization == null)
        {
            throw new BusinessException(Localizer.OAuthInvalidRefreshToken);
        }

        // Check expiration
        if (tokenEntity.ExpirationDate < DateTimeOffset.UtcNow)
        {
            throw new BusinessException(Localizer.OAuthRefreshTokenExpired);
        }

        // Validate client
        if (
            !string.IsNullOrEmpty(request.ClientId)
            && tokenEntity.Authorization.Client.ClientId != request.ClientId
        )
        {
            throw new BusinessException(Localizer.OAuthClientMismatch);
        }

        // Get user
        var user = await _dbContext.Users.FirstOrDefaultAsync(u =>
            u.Id.ToString() == tokenEntity.SubjectId
        );
        if (user == null)
        {
            throw new BusinessException(Localizer.UserNotFound);
        }

        // Generate new tokens
        return await GenerateTokensAsync(
            user,
            tokenEntity.Authorization.Client,
            tokenEntity.Authorization.Scopes,
            signingKey,
            tokenEntity.AuthorizationId
        );
    }

    private async Task<Client> GetValidatedClientAsync(
       string? clientId,
       string? clientSecret,
       string missingDescription = "Missing client credentials"
   )
    {
        if (string.IsNullOrEmpty(clientId))
        {
            throw new BusinessException(Localizer.BadRequest);
        }

        var client = await ValidateClientAsync(clientId, clientSecret);
        return client ?? throw new BusinessException(Localizer.OAuthInvalidClient);
    }

    private async Task<Guid> CreateAuthorizationAsync(
        Client client,
        string subjectId,
        string type,
        string? scopes,
        DateTimeOffset expirationDate,
        CancellationToken cancellationToken = default
    )
    {
        var authorization = new Authorization
        {
            SubjectId = subjectId,
            ClientId = client.Id,
            Type = type,
            Status = AuthorizationStatuses.Valid,
            Scopes = scopes,
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = expirationDate,
        };

        await _dbContext.Authorizations.AddAsync(authorization, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return authorization.Id;
    }

    private static IEnumerable<Claim> BuildAudienceClaims(Client client)
    {
        var audiences = client.ClientResources
            .Select(cr => cr.ApiResource?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return audiences.Select(aud => new Claim(OAuthConst.ClaimTypes.Audience, aud!));
    }

    /// <summary>
    /// Process client credentials grant
    /// </summary>
    private async Task<TokenResponseDto> ProcessClientCredentialsGrantAsync(
        TokenRequestDto request,
        SigningKey signingKey
    )
    {
        var client = await GetValidatedClientAsync(
            request.ClientId,
            request.ClientSecret,
            missingDescription: "Missing client credentials"
        );

        var authorizationId = await CreateAuthorizationAsync(
            client,
            subjectId: client.Id.ToString(),
            type: AuthorizationTypes.ClientCredentials,
            scopes: request.Scope,
            expirationDate: DateTimeOffset.UtcNow.AddHours(1)
        );

        // Generate access token
        var claims = new List<Claim>
        {
            new(OAuthConst.ClaimTypes.Subject, client.Id.ToString()),
            new(OAuthConst.ClaimTypes.ClientId, client.ClientId),
            new(OAuthConst.ClaimTypes.Scope, request.Scope ?? ""),
        };

        claims.AddRange(BuildAudienceClaims(client));

        var accessToken = _oauthService.GenerateToken(claims, signingKey, 3600);
        var refreshTokenValue = OAuthService.GenerateTokenReference();

        // Store token
        var token = new Token
        {
            AuthorizationId = authorizationId,
            ReferenceId = OAuthService.GenerateTokenReference(),
            Type = TokenTypes.AccessToken,
            Status = TokenStatuses.Valid,
            SubjectId = client.Id.ToString(),
            Payload = accessToken,
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = DateTimeOffset.UtcNow.AddHours(1),
        };

        await _dbContext.Tokens.AddAsync(token);
        await _dbContext.SaveChangesAsync();

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            TokenType = TokenTypes.Bearer,
            ExpiresIn = 3600,
            Scope = request.Scope,
        };
    }

    /// <summary>
    /// Process password grant
    /// </summary>
    private async Task<TokenResponseDto> ProcessPasswordGrantAsync(
        TokenRequestDto request,
        SigningKey signingKey
    )
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            throw new BusinessException(Localizer.OAuthMissingUsernameOrPassword);
        }

        // Validate client
        var client = await GetValidatedClientAsync(
            request.ClientId,
            request.ClientSecret,
            missingDescription: "Missing client credentials"
        );

        // Find user
        var user = await _dbContext.Users.FirstOrDefaultAsync(u =>
            u.NormalizedUserName == request.Username.ToUpper()
        );

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            throw new BusinessException(Localizer.InvalidUserOrPassword);
        }

        // Verify password
        var passwordValid = _passwordHasher.VerifyPassword(user.PasswordHash, request.Password);
        if (!passwordValid)
        {
            throw new BusinessException(Localizer.InvalidUserOrPassword);
        }

        // Generate tokens
        return await GenerateTokensAsync(user, client, request.Scope, signingKey);
    }

    /// <summary>
    /// Process device code grant
    /// </summary>
    private async Task<TokenResponseDto> ProcessDeviceCodeGrantAsync(
        TokenRequestDto request,
        SigningKey signingKey
    )
    {
        if (string.IsNullOrEmpty(request.DeviceCode))
        {
            throw new BusinessException(Localizer.OAuthMissingDeviceCode);
        }

        // Find device code
        var tokenEntity = await _dbContext
            .Tokens.Include(t => t.Authorization)
            .ThenInclude(a => a!.Client)
            .FirstOrDefaultAsync(t =>
                t.ReferenceId == request.DeviceCode && t.Type == TokenTypes.DeviceCode
            );

        if (tokenEntity == null || tokenEntity.Authorization == null)
        {
            throw new BusinessException(Localizer.OAuthInvalidDeviceCode);
        }

        // Check if pending
        if (tokenEntity.Status == TokenStatuses.Pending)
        {
            throw new BusinessException(Localizer.OAuthAuthorizationPending);
        }

        // Check if denied
        if (tokenEntity.Status == TokenStatuses.Denied)
        {
            throw new BusinessException(Localizer.OAuthAccessDenied);
        }

        // Check expiration
        if (tokenEntity.ExpirationDate < DateTimeOffset.UtcNow)
        {
            throw new BusinessException(Localizer.OAuthDeviceCodeExpired);
        }

        // Get user
        var user = await _dbContext.Users.FirstOrDefaultAsync(u =>
            u.Id.ToString() == tokenEntity.SubjectId
        );
        if (user == null)
        {
            throw new BusinessException(Localizer.UserNotFound);
        }

        // Mark as redeemed
        tokenEntity.Status = TokenStatuses.Redeemed;
        tokenEntity.RedemptionDate = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        // Generate tokens
        return await GenerateTokensAsync(
            user,
            tokenEntity.Authorization.Client,
            tokenEntity.Authorization.Scopes,
            signingKey,
            tokenEntity.AuthorizationId
        );
    }

    /// <summary>
    /// Generate access and refresh tokens
    /// </summary>
    private async Task<TokenResponseDto> GenerateTokensAsync(
        User user,
        Client client,
        string? scope,
        SigningKey signingKey,
        Guid? authorizationId = null
    )
    {
        // Build claims
        var claims = new List<Claim>
        {
            new(OAuthConst.ClaimTypes.Subject, user.Id.ToString()),
            new(OAuthConst.ClaimTypes.Name, user.UserName),
            new(OAuthConst.ClaimTypes.ClientId, client.ClientId),
        };

        claims.AddRange(BuildAudienceClaims(client));

        if (!string.IsNullOrEmpty(user.Email))
        {
            claims.Add(new Claim(OAuthConst.ClaimTypes.Email, user.Email));
        }

        if (!string.IsNullOrEmpty(scope))
        {
            claims.Add(new Claim(OAuthConst.ClaimTypes.Scope, scope));
        }

        // Generate access token
        var accessToken = _oauthService.GenerateToken(claims, signingKey, 3600);
        var refreshTokenValue = OAuthService.GenerateTokenReference();

        // Create authorization if not exists
        if (!authorizationId.HasValue)
        {
            authorizationId = await CreateAuthorizationAsync(
                client,
                subjectId: user.Id.ToString(),
                type: AuthorizationTypes.Password,
                scopes: scope,
                expirationDate: DateTimeOffset.UtcNow.AddDays(30)
            );
        }

        // Store access token
        var accessTokenEntity = new Token
        {
            AuthorizationId = authorizationId,
            ReferenceId = OAuthService.GenerateTokenReference(),
            Type = TokenTypes.AccessToken,
            Status = TokenStatuses.Valid,
            SubjectId = user.Id.ToString(),
            Payload = accessToken,
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = DateTimeOffset.UtcNow.AddHours(1),
        };

        // Store refresh token
        var refreshTokenEntity = new Token
        {
            AuthorizationId = authorizationId,
            ReferenceId = refreshTokenValue,
            Type = TokenTypes.RefreshToken,
            Status = TokenStatuses.Valid,
            SubjectId = user.Id.ToString(),
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(30),
        };

        await _dbContext.Tokens.AddAsync(accessTokenEntity);
        await _dbContext.Tokens.AddAsync(refreshTokenEntity);
        await _dbContext.SaveChangesAsync();

        // Generate ID token if openid scope is present
        string? idToken = null;
        if (scope?.Contains(Scopes.OpenId) == true)
        {
            var idClaims = new List<Claim>
            {
                new(OAuthConst.ClaimTypes.Subject, user.Id.ToString()),
                new(OAuthConst.ClaimTypes.Name, user.UserName),
            };

            idClaims.AddRange(BuildAudienceClaims(client));

            if (!string.IsNullOrEmpty(user.Email))
            {
                idClaims.Add(new Claim(OAuthConst.ClaimTypes.Email, user.Email));
            }

            idToken = _oauthService.GenerateToken(idClaims, signingKey, 3600);
        }

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            TokenType = TokenTypes.Bearer,
            ExpiresIn = 3600,
            RefreshToken = refreshTokenValue,
            IdToken = idToken,
            Scope = scope,
        };
    }

    /// <summary>
    /// Validate client credentials
    /// </summary>
    private async Task<Client?> ValidateClientAsync(string? clientId, string? clientSecret)
    {
        if (string.IsNullOrEmpty(clientId))
        {
            return null;
        }

        var client = await _dbContext
            .Clients.Include(c => c.ClientScopes)
            .ThenInclude(cs => cs.Scope)
            .Include(c => c.ClientResources)
            .ThenInclude(cr => cr.ApiResource)
            .FirstOrDefaultAsync(c => c.ClientId == clientId);

        if (client == null)
        {
            return null;
        }

        // If client has a secret, validate it
        if (!string.IsNullOrEmpty(client.ClientSecret))
        {
            if (string.IsNullOrEmpty(clientSecret))
            {
                return null;
            }

            var secretValid = _passwordHasher.VerifyPassword(client.ClientSecret, clientSecret);
            if (!secretValid)
            {
                return null;
            }
        }

        return client;
    }

    /// <summary>
    /// Revoke token
    /// </summary>
    public async Task<bool> RevokeTokenAsync(string token, string? tokenTypeHint)
    {
        var tokenEntity = await _dbContext.Tokens.FirstOrDefaultAsync(t =>
            t.ReferenceId == token || t.Payload == token
        );

        if (tokenEntity == null)
        {
            return true; // Token doesn't exist, consider it revoked
        }

        tokenEntity.Status = TokenStatuses.Revoked;
        await _dbContext.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Introspect token
    /// </summary>
    public async Task<IntrospectResponseDto> IntrospectTokenAsync(
        string token,
        string? tokenTypeHint
    )
    {
        var tokenEntity = await _dbContext
            .Tokens.Include(t => t.Authorization)
            .ThenInclude(a => a!.Client)
            .FirstOrDefaultAsync(t => t.ReferenceId == token || t.Payload == token);

        if (tokenEntity == null || tokenEntity.Status != TokenStatuses.Valid)
        {
            return new IntrospectResponseDto { Active = false };
        }

        // Check expiration
        if (tokenEntity.ExpirationDate < DateTimeOffset.UtcNow)
        {
            return new IntrospectResponseDto { Active = false };
        }

        var response = new IntrospectResponseDto
        {
            Active = true,
            Scope = tokenEntity.Authorization?.Scopes,
            ClientId = tokenEntity.Authorization?.Client.ClientId,
            TokenType = tokenEntity.Type,
            Sub = tokenEntity.SubjectId,
            Iat = tokenEntity.CreationDate.ToUnixTimeSeconds(),
        };

        if (tokenEntity.ExpirationDate.HasValue)
        {
            response.Exp = tokenEntity.ExpirationDate.Value.ToUnixTimeSeconds();
        }

        return response;
    }
}
