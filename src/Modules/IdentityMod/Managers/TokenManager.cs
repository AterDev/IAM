using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Ater.Web.Convention.Services;
using IdentityMod.Models.OAuthDtos;
using IdentityMod.Services;

namespace IdentityMod.Managers;

/// <summary>
/// Manager for OAuth/OIDC token operations
/// </summary>
public class TokenManager(
    DefaultDbContext dbContext,
    ILogger<TokenManager> logger,
    JwtService jwtService,
    IPasswordHasher passwordHasher,
    OAuthService oAuthService
    ) : ManagerBase<DefaultDbContext>(dbContext, logger)
{
    private readonly JwtService _jwtService = jwtService;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly OAuthService _oAuthService = oAuthService;

    /// <summary>
    /// Process token request (authorization_code, refresh_token, client_credentials, password)
    /// </summary>
    public async Task<TokenResponseDto> ProcessTokenRequestAsync(TokenRequestDto request)
    {
        return request.GrantType switch
        {
            GrantTypes.AuthorizationCode => await ProcessAuthorizationCodeGrantAsync(request),
            GrantTypes.RefreshToken => await ProcessRefreshTokenGrantAsync(request),
            GrantTypes.ClientCredentials => await ProcessClientCredentialsGrantAsync(request),
            GrantTypes.Password => await ProcessPasswordGrantAsync(request),
            GrantTypes.DeviceCode => await ProcessDeviceCodeGrantAsync(request),
            _ => new TokenResponseDto
            {
                Error = ErrorCodes.UnsupportedGrantType,
                ErrorDescription = "The grant type is not supported"
            }
        };
    }

    /// <summary>
    /// Process authorization code grant
    /// </summary>
    private async Task<TokenResponseDto> ProcessAuthorizationCodeGrantAsync(TokenRequestDto request)
    {
        if (string.IsNullOrEmpty(request.Code) || string.IsNullOrEmpty(request.ClientId))
        {
            return new TokenResponseDto
            {
                Error = ErrorCodes.InvalidRequest,
                ErrorDescription = "Missing required parameters"
            };
        }

        // Validate client
        var client = await ValidateClientAsync(request.ClientId, request.ClientSecret);
        if (client == null)
        {
            return new TokenResponseDto
            {
                Error = ErrorCodes.InvalidClient,
                ErrorDescription = "Invalid client credentials"
            };
        }

        // Validate authorization code
        var token = await _dbContext.Tokens
            .Include(t => t.Authorization)
                .ThenInclude(a => a!.Client)
            .FirstOrDefaultAsync(t =>
                t.ReferenceId == request.Code &&
                t.Type == TokenTypes.AuthorizationCode &&
                t.Status == TokenStatuses.Valid
            );

        if (token == null || token.Authorization == null)
        {
            return new TokenResponseDto
            {
                Error = ErrorCodes.InvalidGrant,
                ErrorDescription = "Invalid authorization code"
            };
        }

        // Check expiration
        if (token.ExpirationDate < DateTimeOffset.UtcNow)
        {
            return new TokenResponseDto
            {
                Error = ErrorCodes.InvalidGrant,
                ErrorDescription = "Authorization code expired"
            };
        }

        // Validate client
        if (token.Authorization.Client.ClientId != request.ClientId)
        {
            return new TokenResponseDto
            {
                Error = ErrorCodes.InvalidClient,
                ErrorDescription = "Client mismatch"
            };
        }

        // Validate redirect URI
        var properties = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(
            token.Authorization.Properties ?? "{}"
        );
        if (properties?.GetValueOrDefault("redirect_uri") != request.RedirectUri)
        {
            return new TokenResponseDto
            {
                Error = ErrorCodes.InvalidRequest,
                ErrorDescription = "Invalid redirect URI"
            };
        }

        // Validate PKCE if present
        var codeChallenge = properties?.GetValueOrDefault("code_challenge");
        var codeChallengeMethod = properties?.GetValueOrDefault("code_challenge_method");

        if (!string.IsNullOrEmpty(codeChallenge))
        {
            if (string.IsNullOrEmpty(request.CodeVerifier))
            {
                return new TokenResponseDto
                {
                    Error = ErrorCodes.InvalidRequest,
                    ErrorDescription = "Missing code verifier"
                };
            }

            var isValidPkce = _oAuthService.ValidatePkce(request.CodeVerifier, codeChallenge, codeChallengeMethod ?? CodeChallengeMethods.Plain);
            if (!isValidPkce)
            {
                return new TokenResponseDto
                {
                    Error = ErrorCodes.InvalidGrant,
                    ErrorDescription = "Invalid code verifier"
                };
            }
        }

        // Mark code as redeemed
        token.Status = TokenStatuses.Redeemed;
        token.RedemptionDate = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        var authorization = token.Authorization;

        // Get user
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id.ToString() == authorization.SubjectId);
        if (user == null)
        {
            return new TokenResponseDto
            {
                Error = ErrorCodes.InvalidGrant,
                ErrorDescription = "User not found"
            };
        }

        // Generate tokens
        return await GenerateTokensAsync(user, client, authorization.Scopes, authorization.Id);
    }

    /// <summary>
    /// Process refresh token grant
    /// </summary>
    private async Task<TokenResponseDto> ProcessRefreshTokenGrantAsync(TokenRequestDto request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return new TokenResponseDto
            {
                Error = ErrorCodes.InvalidRequest,
                ErrorDescription = "Missing refresh token"
            };
        }

        // Find refresh token
        var tokenEntity = await _dbContext.Tokens
            .Include(t => t.Authorization)
                .ThenInclude(a => a!.Client)
            .FirstOrDefaultAsync(t =>
                t.ReferenceId == request.RefreshToken &&
                t.Type == TokenTypes.RefreshToken &&
                t.Status == TokenStatuses.Valid
            );

        if (tokenEntity == null || tokenEntity.Authorization == null)
        {
            return new TokenResponseDto
            {
                Error = ErrorCodes.InvalidGrant,
                ErrorDescription = "Invalid refresh token"
            };
        }

        // Check expiration
        if (tokenEntity.ExpirationDate < DateTimeOffset.UtcNow)
        {
            return new TokenResponseDto
            {
                Error = ErrorCodes.InvalidGrant,
                ErrorDescription = "Refresh token expired"
            };
        }

        // Validate client
        if (!string.IsNullOrEmpty(request.ClientId) &&
            tokenEntity.Authorization.Client.ClientId != request.ClientId)
        {
            return new TokenResponseDto
            {
                Error = ErrorCodes.InvalidClient,
                ErrorDescription = "Client mismatch"
            };
        }

        // Get user
        var user = await _dbContext.Users.FirstOrDefaultAsync(u =>
            u.Id.ToString() == tokenEntity.SubjectId
        );
        if (user == null)
        {
            return new TokenResponseDto
            {
                Error = ErrorCodes.InvalidGrant,
                ErrorDescription = "User not found"
            };
        }

        // Generate new tokens
        return await GenerateTokensAsync(
            user,
            tokenEntity.Authorization.Client,
            tokenEntity.Authorization.Scopes,
            tokenEntity.AuthorizationId
        );
    }

    /// <summary>
    /// Process client credentials grant
    /// </summary>
    private async Task<TokenResponseDto> ProcessClientCredentialsGrantAsync(TokenRequestDto request)
    {
        if (string.IsNullOrEmpty(request.ClientId) || string.IsNullOrEmpty(request.ClientSecret))
        {
            return new TokenResponseDto
            {
                Error = ErrorCodes.InvalidRequest,
                ErrorDescription = "Missing client credentials"
            };
        }

        // Validate client
        var client = await ValidateClientAsync(request.ClientId, request.ClientSecret);
        if (client == null)
        {
            return new TokenResponseDto
            {
                Error = ErrorCodes.InvalidClient,
                ErrorDescription = "Invalid client credentials"
            };
        }

        // Create authorization for client
        var authorization = new Authorization
        {
            SubjectId = client.Id.ToString(),
            ClientId = client.Id,
            Type = AuthorizationTypes.ClientCredentials,
            Status = AuthorizationStatuses.Valid,
            Scopes = request.Scope,
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = DateTimeOffset.UtcNow.AddHours(1)
        };

        await _dbContext.Authorizations.AddAsync(authorization);
        await _dbContext.SaveChangesAsync();

        // Generate access token
        var claims = new List<Claim>
        {
            new(OAuthConstants.ClaimTypes.Subject, client.Id.ToString()),
            new(OAuthConstants.ClaimTypes.ClientId, client.ClientId),
            new(OAuthConstants.ClaimTypes.Scope, request.Scope ?? "")
        };

        var accessToken = _jwtService.GetToken(claims, 3600);
        var refreshTokenValue = _oAuthService.GenerateTokenReference();

        // Store token
        var token = new Token
        {
            AuthorizationId = authorization.Id,
            ReferenceId = _oAuthService.GenerateTokenReference(),
            Type = TokenTypes.AccessToken,
            Status = TokenStatuses.Valid,
            SubjectId = client.Id.ToString(),
            Payload = accessToken,
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = DateTimeOffset.UtcNow.AddHours(1)
        };

        await _dbContext.Tokens.AddAsync(token);
        await _dbContext.SaveChangesAsync();

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            TokenType = TokenTypes.Bearer,
            ExpiresIn = 3600,
            Scope = request.Scope
        };
    }

    /// <summary>
    /// Process password grant
    /// </summary>
    private async Task<TokenResponseDto> ProcessPasswordGrantAsync(TokenRequestDto request)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            return new TokenResponseDto
            {
                Error = ErrorCodes.InvalidRequest,
                ErrorDescription = "Missing username or password"
            };
        }

        // Validate client
        var client = await ValidateClientAsync(request.ClientId, request.ClientSecret);
        if (client == null)
        {
            return new TokenResponseDto
            {
                Error = ErrorCodes.InvalidClient,
                ErrorDescription = "Invalid client credentials"
            };
        }

        // Find user
        var user = await _dbContext.Users.FirstOrDefaultAsync(u =>
            u.NormalizedUserName == request.Username.ToUpper()
        );

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            return new TokenResponseDto
            {
                Error = ErrorCodes.InvalidGrant,
                ErrorDescription = "Invalid username or password"
            };
        }

        // Verify password
        var passwordValid = _passwordHasher.VerifyPassword(user.PasswordHash, request.Password);
        if (!passwordValid)
        {
            return new TokenResponseDto
            {
                Error = ErrorCodes.InvalidGrant,
                ErrorDescription = "Invalid username or password"
            };
        }

        // Generate tokens
        return await GenerateTokensAsync(user, client, request.Scope);
    }

    /// <summary>
    /// Process device code grant
    /// </summary>
    private async Task<TokenResponseDto> ProcessDeviceCodeGrantAsync(TokenRequestDto request)
    {
        if (string.IsNullOrEmpty(request.DeviceCode))
        {
            return new TokenResponseDto
            {
                Error = ErrorCodes.InvalidRequest,
                ErrorDescription = "Missing device code"
            };
        }

        // Find device code
        var tokenEntity = await _dbContext.Tokens
            .Include(t => t.Authorization)
                .ThenInclude(a => a!.Client)
            .FirstOrDefaultAsync(t =>
                t.ReferenceId == request.DeviceCode &&
                t.Type == TokenTypes.DeviceCode
            );

        if (tokenEntity == null || tokenEntity.Authorization == null)
        {
            return new TokenResponseDto
            {
                Error = ErrorCodes.InvalidGrant,
                ErrorDescription = "Invalid device code"
            };
        }

        // Check if pending
        if (tokenEntity.Status == TokenStatuses.Pending)
        {
            return new TokenResponseDto
            {
                Error = ErrorCodes.AuthorizationPending,
                ErrorDescription = "User has not yet authorized the device"
            };
        }

        // Check if denied
        if (tokenEntity.Status == TokenStatuses.Denied)
        {
            return new TokenResponseDto
            {
                Error = ErrorCodes.AccessDenied,
                ErrorDescription = "User denied the authorization"
            };
        }

        // Check expiration
        if (tokenEntity.ExpirationDate < DateTimeOffset.UtcNow)
        {
            return new TokenResponseDto
            {
                Error = ErrorCodes.ExpiredToken,
                ErrorDescription = "Device code expired"
            };
        }

        // Get user
        var user = await _dbContext.Users.FirstOrDefaultAsync(u =>
            u.Id.ToString() == tokenEntity.SubjectId
        );
        if (user == null)
        {
            return new TokenResponseDto
            {
                Error = ErrorCodes.InvalidGrant,
                ErrorDescription = "User not found"
            };
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
        Guid? authorizationId = null
    )
    {
        // Build claims
        var claims = new List<Claim>
        {
            new(OAuthConstants.ClaimTypes.Subject, user.Id.ToString()),
            new(OAuthConstants.ClaimTypes.Name, user.UserName),
            new(OAuthConstants.ClaimTypes.ClientId, client.ClientId)
        };

        if (!string.IsNullOrEmpty(user.Email))
        {
            claims.Add(new Claim(OAuthConstants.ClaimTypes.Email, user.Email));
        }

        if (!string.IsNullOrEmpty(scope))
        {
            claims.Add(new Claim(OAuthConstants.ClaimTypes.Scope, scope));
        }

        // Generate access token
        var accessToken = _jwtService.GetToken(claims, 3600);
        var refreshTokenValue = _oAuthService.GenerateTokenReference();

        // Create authorization if not exists
        if (!authorizationId.HasValue)
        {
            var authorization = new Authorization
            {
                SubjectId = user.Id.ToString(),
                ClientId = client.Id,
                Type = AuthorizationTypes.Password,
                Status = AuthorizationStatuses.Valid,
                Scopes = scope,
                CreationDate = DateTimeOffset.UtcNow,
                ExpirationDate = DateTimeOffset.UtcNow.AddDays(30)
            };

            await _dbContext.Authorizations.AddAsync(authorization);
            await _dbContext.SaveChangesAsync();
            authorizationId = authorization.Id;
        }

        // Store access token
        var accessTokenEntity = new Token
        {
            AuthorizationId = authorizationId,
            ReferenceId = _oAuthService.GenerateTokenReference(),
            Type = TokenTypes.AccessToken,
            Status = TokenStatuses.Valid,
            SubjectId = user.Id.ToString(),
            Payload = accessToken,
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = DateTimeOffset.UtcNow.AddHours(1)
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
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(30)
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
                new(OAuthConstants.ClaimTypes.Subject, user.Id.ToString()),
                new(OAuthConstants.ClaimTypes.Name, user.UserName),
                new(OAuthConstants.ClaimTypes.Audience, client.ClientId)
            };

            if (!string.IsNullOrEmpty(user.Email))
            {
                idClaims.Add(new Claim(OAuthConstants.ClaimTypes.Email, user.Email));
            }

            idToken = _jwtService.GetToken(idClaims, 3600);
        }

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            TokenType = TokenTypes.Bearer,
            ExpiresIn = 3600,
            RefreshToken = refreshTokenValue,
            IdToken = idToken,
            Scope = scope
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

        var client = await _dbContext.Clients
            .Include(c => c.ClientScopes)
                .ThenInclude(cs => cs.Scope)
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
    public async Task<IntrospectResponseDto> IntrospectTokenAsync(string token, string? tokenTypeHint)
    {
        var tokenEntity = await _dbContext.Tokens
            .Include(t => t.Authorization)
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
