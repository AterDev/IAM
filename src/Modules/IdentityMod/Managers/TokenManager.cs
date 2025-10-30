using System.Security.Claims;
using System.Security.Cryptography;
using IdentityMod.Models.OAuthDtos;
using Ater.Web.Convention.Services;

namespace IdentityMod.Managers;

/// <summary>
/// Manager for OAuth/OIDC token operations
/// </summary>
public class TokenManager(
    DefaultDbContext dbContext,
    ILogger<TokenManager> logger,
    JwtService jwtService,
    IPasswordHasher passwordHasher
    ) : ManagerBase<DefaultDbContext>(dbContext, logger)
{
    private readonly JwtService _jwtService = jwtService;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;

    /// <summary>
    /// Process token request (authorization_code, refresh_token, client_credentials, password)
    /// </summary>
    public async Task<TokenResponseDto> ProcessTokenRequestAsync(TokenRequestDto request)
    {
        return request.GrantType switch
        {
            OAuthConstants.GrantTypes.AuthorizationCode => await ProcessAuthorizationCodeGrantAsync(request),
            OAuthConstants.GrantTypes.RefreshToken => await ProcessRefreshTokenGrantAsync(request),
            OAuthConstants.GrantTypes.ClientCredentials => await ProcessClientCredentialsGrantAsync(request),
            OAuthConstants.GrantTypes.Password => await ProcessPasswordGrantAsync(request),
            OAuthConstants.GrantTypes.DeviceCode => await ProcessDeviceCodeGrantAsync(request),
            _ => new TokenResponseDto
            {
                Error = OAuthConstants.ErrorCodes.UnsupportedGrantType,
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
                Error = OAuthConstants.ErrorCodes.InvalidRequest,
                ErrorDescription = "Missing required parameters"
            };
        }

        // Validate client
        var client = await ValidateClientAsync(request.ClientId, request.ClientSecret);
        if (client == null)
        {
            return new TokenResponseDto
            {
                Error = OAuthConstants.ErrorCodes.InvalidClient,
                ErrorDescription = "Invalid client credentials"
            };
        }

        // Validate authorization code
        var authManager = new AuthorizationManager(_dbContext, _logger as ILogger<AuthorizationManager>);
        var (isValid, authorization) = await authManager.ValidateAuthorizationCodeAsync(
            request.Code,
            request.ClientId,
            request.RedirectUri ?? "",
            request.CodeVerifier
        );

        if (!isValid || authorization == null)
        {
            return new TokenResponseDto
            {
                Error = OAuthConstants.ErrorCodes.InvalidGrant,
                ErrorDescription = "Invalid authorization code"
            };
        }

        // Get user
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id.ToString() == authorization.SubjectId);
        if (user == null)
        {
            return new TokenResponseDto
            {
                Error = OAuthConstants.ErrorCodes.InvalidGrant,
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
                Error = OAuthConstants.ErrorCodes.InvalidRequest,
                ErrorDescription = "Missing refresh token"
            };
        }

        // Find refresh token
        var tokenEntity = await _dbContext.Tokens
            .Include(t => t.Authorization)
                .ThenInclude(a => a!.Client)
            .FirstOrDefaultAsync(t =>
                t.ReferenceId == request.RefreshToken &&
                t.Type == OAuthConstants.TokenTypes.RefreshToken &&
                t.Status == OAuthConstants.TokenStatuses.Valid
            );

        if (tokenEntity == null || tokenEntity.Authorization == null)
        {
            return new TokenResponseDto
            {
                Error = OAuthConstants.ErrorCodes.InvalidGrant,
                ErrorDescription = "Invalid refresh token"
            };
        }

        // Check expiration
        if (tokenEntity.ExpirationDate < DateTimeOffset.UtcNow)
        {
            return new TokenResponseDto
            {
                Error = OAuthConstants.ErrorCodes.InvalidGrant,
                ErrorDescription = "Refresh token expired"
            };
        }

        // Validate client
        if (!string.IsNullOrEmpty(request.ClientId) &&
            tokenEntity.Authorization.Client.ClientId != request.ClientId)
        {
            return new TokenResponseDto
            {
                Error = OAuthConstants.ErrorCodes.InvalidClient,
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
                Error = OAuthConstants.ErrorCodes.InvalidGrant,
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
                Error = OAuthConstants.ErrorCodes.InvalidRequest,
                ErrorDescription = "Missing client credentials"
            };
        }

        // Validate client
        var client = await ValidateClientAsync(request.ClientId, request.ClientSecret);
        if (client == null)
        {
            return new TokenResponseDto
            {
                Error = OAuthConstants.ErrorCodes.InvalidClient,
                ErrorDescription = "Invalid client credentials"
            };
        }

        // Create authorization for client
        var authorization = new Authorization
        {
            SubjectId = client.Id.ToString(),
            ClientId = client.Id,
            Type = OAuthConstants.AuthorizationTypes.ClientCredentials,
            Status = OAuthConstants.AuthorizationStatuses.Valid,
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

        // Store token
        var token = new Token
        {
            AuthorizationId = authorization.Id,
            ReferenceId = GenerateTokenReference(),
            Type = OAuthConstants.TokenTypes.AccessToken,
            Status = OAuthConstants.TokenStatuses.Valid,
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
            TokenType = OAuthConstants.TokenTypes.Bearer,
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
                Error = OAuthConstants.ErrorCodes.InvalidRequest,
                ErrorDescription = "Missing username or password"
            };
        }

        // Validate client
        var client = await ValidateClientAsync(request.ClientId, request.ClientSecret);
        if (client == null)
        {
            return new TokenResponseDto
            {
                Error = OAuthConstants.ErrorCodes.InvalidClient,
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
                Error = OAuthConstants.ErrorCodes.InvalidGrant,
                ErrorDescription = "Invalid username or password"
            };
        }

        // Verify password
        var passwordValid = _passwordHasher.VerifyPassword(user.PasswordHash, request.Password);
        if (!passwordValid)
        {
            return new TokenResponseDto
            {
                Error = OAuthConstants.ErrorCodes.InvalidGrant,
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
                Error = OAuthConstants.ErrorCodes.InvalidRequest,
                ErrorDescription = "Missing device code"
            };
        }

        // Find device code
        var tokenEntity = await _dbContext.Tokens
            .Include(t => t.Authorization)
                .ThenInclude(a => a!.Client)
            .FirstOrDefaultAsync(t =>
                t.ReferenceId == request.DeviceCode &&
                t.Type == OAuthConstants.TokenTypes.DeviceCode
            );

        if (tokenEntity == null || tokenEntity.Authorization == null)
        {
            return new TokenResponseDto
            {
                Error = OAuthConstants.ErrorCodes.InvalidGrant,
                ErrorDescription = "Invalid device code"
            };
        }

        // Check if pending
        if (tokenEntity.Status == OAuthConstants.TokenStatuses.Pending)
        {
            return new TokenResponseDto
            {
                Error = OAuthConstants.ErrorCodes.AuthorizationPending,
                ErrorDescription = "User has not yet authorized the device"
            };
        }

        // Check if denied
        if (tokenEntity.Status == OAuthConstants.TokenStatuses.Denied)
        {
            return new TokenResponseDto
            {
                Error = OAuthConstants.ErrorCodes.AccessDenied,
                ErrorDescription = "User denied the authorization"
            };
        }

        // Check expiration
        if (tokenEntity.ExpirationDate < DateTimeOffset.UtcNow)
        {
            return new TokenResponseDto
            {
                Error = OAuthConstants.ErrorCodes.ExpiredToken,
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
                Error = OAuthConstants.ErrorCodes.InvalidGrant,
                ErrorDescription = "User not found"
            };
        }

        // Mark as redeemed
        tokenEntity.Status = OAuthConstants.TokenStatuses.Redeemed;
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
        var refreshTokenValue = GenerateTokenReference();

        // Create authorization if not exists
        if (!authorizationId.HasValue)
        {
            var authorization = new Authorization
            {
                SubjectId = user.Id.ToString(),
                ClientId = client.Id,
                Type = OAuthConstants.AuthorizationTypes.Password,
                Status = OAuthConstants.AuthorizationStatuses.Valid,
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
            ReferenceId = GenerateTokenReference(),
            Type = OAuthConstants.TokenTypes.AccessToken,
            Status = OAuthConstants.TokenStatuses.Valid,
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
            Type = OAuthConstants.TokenTypes.RefreshToken,
            Status = OAuthConstants.TokenStatuses.Valid,
            SubjectId = user.Id.ToString(),
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(30)
        };

        await _dbContext.Tokens.AddAsync(accessTokenEntity);
        await _dbContext.Tokens.AddAsync(refreshTokenEntity);
        await _dbContext.SaveChangesAsync();

        // Generate ID token if openid scope is present
        string? idToken = null;
        if (scope?.Contains(OAuthConstants.Scopes.OpenId) == true)
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
            TokenType = OAuthConstants.TokenTypes.Bearer,
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

        tokenEntity.Status = OAuthConstants.TokenStatuses.Revoked;
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

        if (tokenEntity == null || tokenEntity.Status != OAuthConstants.TokenStatuses.Valid)
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

    /// <summary>
    /// Generate token reference
    /// </summary>
    private string GenerateTokenReference()
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
