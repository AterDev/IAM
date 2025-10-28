using System.Security.Claims;
using System.Security.Cryptography;
using IdentityMod.Models.OAuthDtos;

namespace IdentityMod.Managers;

/// <summary>
/// Manager for OAuth/OIDC token operations
/// </summary>
public class TokenManager(
    DefaultDbContext dbContext,
    ILogger<TokenManager> logger,
    IJwtTokenService jwtTokenService,
    IPasswordHasher passwordHasher
    ) : ManagerBase<DefaultDbContext>(dbContext, logger)
{
    private readonly IJwtTokenService _jwtTokenService = jwtTokenService;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;

    /// <summary>
    /// Process token request (authorization_code, refresh_token, client_credentials, password)
    /// </summary>
    public async Task<TokenResponseDto> ProcessTokenRequestAsync(TokenRequestDto request)
    {
        return request.GrantType switch
        {
            "authorization_code" => await ProcessAuthorizationCodeGrantAsync(request),
            "refresh_token" => await ProcessRefreshTokenGrantAsync(request),
            "client_credentials" => await ProcessClientCredentialsGrantAsync(request),
            "password" => await ProcessPasswordGrantAsync(request),
            "urn:ietf:params:oauth:grant-type:device_code" => await ProcessDeviceCodeGrantAsync(request),
            _ => new TokenResponseDto
            {
                Error = "unsupported_grant_type",
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
                Error = "invalid_request",
                ErrorDescription = "Missing required parameters"
            };
        }

        // Validate client
        var client = await ValidateClientAsync(request.ClientId, request.ClientSecret);
        if (client == null)
        {
            return new TokenResponseDto
            {
                Error = "invalid_client",
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
                Error = "invalid_grant",
                ErrorDescription = "Invalid authorization code"
            };
        }

        // Get user
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id.ToString() == authorization.SubjectId);
        if (user == null)
        {
            return new TokenResponseDto
            {
                Error = "invalid_grant",
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
                Error = "invalid_request",
                ErrorDescription = "Missing refresh token"
            };
        }

        // Find refresh token
        var tokenEntity = await _dbContext.Tokens
            .Include(t => t.Authorization)
                .ThenInclude(a => a!.Client)
            .FirstOrDefaultAsync(t =>
                t.ReferenceId == request.RefreshToken &&
                t.Type == "refresh_token" &&
                t.Status == "valid"
            );

        if (tokenEntity == null || tokenEntity.Authorization == null)
        {
            return new TokenResponseDto
            {
                Error = "invalid_grant",
                ErrorDescription = "Invalid refresh token"
            };
        }

        // Check expiration
        if (tokenEntity.ExpirationDate < DateTimeOffset.UtcNow)
        {
            return new TokenResponseDto
            {
                Error = "invalid_grant",
                ErrorDescription = "Refresh token expired"
            };
        }

        // Validate client
        if (!string.IsNullOrEmpty(request.ClientId) &&
            tokenEntity.Authorization.Client.ClientId != request.ClientId)
        {
            return new TokenResponseDto
            {
                Error = "invalid_client",
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
                Error = "invalid_grant",
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
                Error = "invalid_request",
                ErrorDescription = "Missing client credentials"
            };
        }

        // Validate client
        var client = await ValidateClientAsync(request.ClientId, request.ClientSecret);
        if (client == null)
        {
            return new TokenResponseDto
            {
                Error = "invalid_client",
                ErrorDescription = "Invalid client credentials"
            };
        }

        // Create authorization for client
        var authorization = new Authorization
        {
            SubjectId = client.Id.ToString(),
            ClientId = client.Id,
            Type = "client_credentials",
            Status = "valid",
            Scopes = request.Scope,
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = DateTimeOffset.UtcNow.AddHours(1)
        };

        await _dbContext.Authorizations.AddAsync(authorization);
        await _dbContext.SaveChangesAsync();

        // Generate access token
        var claims = new List<Claim>
        {
            new("sub", client.Id.ToString()),
            new("client_id", client.ClientId),
            new("scope", request.Scope ?? "")
        };

        var accessToken = _jwtTokenService.GenerateAccessToken(claims, 3600);

        // Store token
        var token = new Token
        {
            AuthorizationId = authorization.Id,
            ReferenceId = GenerateTokenReference(),
            Type = "access_token",
            Status = "valid",
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
            TokenType = "Bearer",
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
                Error = "invalid_request",
                ErrorDescription = "Missing username or password"
            };
        }

        // Validate client
        var client = await ValidateClientAsync(request.ClientId, request.ClientSecret);
        if (client == null)
        {
            return new TokenResponseDto
            {
                Error = "invalid_client",
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
                Error = "invalid_grant",
                ErrorDescription = "Invalid username or password"
            };
        }

        // Verify password
        var passwordValid = _passwordHasher.VerifyPassword(user.PasswordHash, request.Password);
        if (!passwordValid)
        {
            return new TokenResponseDto
            {
                Error = "invalid_grant",
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
                Error = "invalid_request",
                ErrorDescription = "Missing device code"
            };
        }

        // Find device code
        var tokenEntity = await _dbContext.Tokens
            .Include(t => t.Authorization)
                .ThenInclude(a => a!.Client)
            .FirstOrDefaultAsync(t =>
                t.ReferenceId == request.DeviceCode &&
                t.Type == "device_code"
            );

        if (tokenEntity == null || tokenEntity.Authorization == null)
        {
            return new TokenResponseDto
            {
                Error = "invalid_grant",
                ErrorDescription = "Invalid device code"
            };
        }

        // Check if pending
        if (tokenEntity.Status == "pending")
        {
            return new TokenResponseDto
            {
                Error = "authorization_pending",
                ErrorDescription = "User has not yet authorized the device"
            };
        }

        // Check if denied
        if (tokenEntity.Status == "denied")
        {
            return new TokenResponseDto
            {
                Error = "access_denied",
                ErrorDescription = "User denied the authorization"
            };
        }

        // Check expiration
        if (tokenEntity.ExpirationDate < DateTimeOffset.UtcNow)
        {
            return new TokenResponseDto
            {
                Error = "expired_token",
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
                Error = "invalid_grant",
                ErrorDescription = "User not found"
            };
        }

        // Mark as redeemed
        tokenEntity.Status = "redeemed";
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
            new("sub", user.Id.ToString()),
            new("name", user.UserName),
            new("client_id", client.ClientId)
        };

        if (!string.IsNullOrEmpty(user.Email))
        {
            claims.Add(new Claim("email", user.Email));
        }

        if (!string.IsNullOrEmpty(scope))
        {
            claims.Add(new Claim("scope", scope));
        }

        // Generate access token
        var accessToken = _jwtTokenService.GenerateAccessToken(claims, 3600);
        var refreshTokenValue = GenerateTokenReference();

        // Create authorization if not exists
        if (!authorizationId.HasValue)
        {
            var authorization = new Authorization
            {
                SubjectId = user.Id.ToString(),
                ClientId = client.Id,
                Type = "password",
                Status = "valid",
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
            Type = "access_token",
            Status = "valid",
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
            Type = "refresh_token",
            Status = "valid",
            SubjectId = user.Id.ToString(),
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(30)
        };

        await _dbContext.Tokens.AddAsync(accessTokenEntity);
        await _dbContext.Tokens.AddAsync(refreshTokenEntity);
        await _dbContext.SaveChangesAsync();

        // Generate ID token if openid scope is present
        string? idToken = null;
        if (scope?.Contains("openid") == true)
        {
            var idClaims = new List<Claim>
            {
                new("sub", user.Id.ToString()),
                new("name", user.UserName),
                new("aud", client.ClientId)
            };

            if (!string.IsNullOrEmpty(user.Email))
            {
                idClaims.Add(new Claim("email", user.Email));
            }

            idToken = _jwtTokenService.GenerateIdToken(idClaims, 3600);
        }

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
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

        tokenEntity.Status = "revoked";
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

        if (tokenEntity == null || tokenEntity.Status != "valid")
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
