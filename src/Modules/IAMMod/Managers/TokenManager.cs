using IAMMod.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Share;
using Share.Exceptions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace IAMMod.Managers;

/// <summary>
/// Manager for OAuth/OIDC token operations
/// </summary>
public class TokenManager(
    DefaultDbContext dbContext,
    ILogger<TokenManager> logger,
    OAuthService oauthService,
    AuditLogManager auditLogManager,
    RiskControlService riskControlService
) : ManagerBase<DefaultDbContext>(dbContext, logger)
{
    private readonly OAuthService _oauthService = oauthService;
    private readonly AuditLogManager _auditLogManager = auditLogManager;
    private readonly RiskControlService _riskControlService = riskControlService;

    /// <summary>
    /// Validate a client for sensitive token management endpoints.
    /// </summary>
    public async Task<Client?> ValidateSensitiveEndpointClientAsync(string? clientId, string? clientSecret)
    {
        return await ValidateClientAsync(clientId, clientSecret);
    }

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
            OpenIdConnectGrantTypes.AuthorizationCode => await ProcessAuthorizationCodeGrantAsync(request, signingKey),
            OpenIdConnectGrantTypes.RefreshToken => await ProcessRefreshTokenGrantAsync(request, signingKey),
            OpenIdConnectGrantTypes.ClientCredentials => await ProcessClientCredentialsGrantAsync(request, signingKey),
            OpenIdConnectGrantTypes.Password => await ProcessPasswordGrantAsync(request, signingKey),
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
        if (properties?.GetValueOrDefault(OpenIdConnectParameterNames.RedirectUri) != request.RedirectUri)
        {
            throw new BusinessException(Localizer.OAuthInvalidRedirectUri);
        }

        // Validate PKCE if present
        var codeChallenge = properties?.GetValueOrDefault("code_challenge");
        var codeChallengeMethod = properties?.GetValueOrDefault("code_challenge_method");
        var sessionId = properties?.GetValueOrDefault(OpenIdConnectParameterNames.Sid);
        var nonce = properties?.GetValueOrDefault(OpenIdConnectParameterNames.Nonce);

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
        var user = await GetUserWithRolesAsync(authorization.SubjectId);
        if (user == null)
        {
            throw new BusinessException(Localizer.UserNotFound);
        }

        // Generate tokens
        return await GenerateTokensAsync(
            user,
            client,
            authorization.Scopes,
            signingKey,
            authorization.Id,
            null,
            sessionId,
            nonce
        );
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
            );

        if (tokenEntity == null || tokenEntity.Authorization == null)
        {
            throw new BusinessException(Localizer.OAuthInvalidRefreshToken);
        }

        if (tokenEntity.Status != TokenStatuses.Valid)
        {
            if (tokenEntity.Status == TokenStatuses.Redeemed)
            {
                await HandleRefreshTokenReuseAsync(tokenEntity);
            }

            throw new BusinessException(Localizer.OAuthInvalidRefreshToken);
        }

        // Check expiration
        if (tokenEntity.ExpirationDate < DateTimeOffset.UtcNow)
        {
            throw new BusinessException(Localizer.OAuthRefreshTokenExpired);
        }

        // Validate client
        var client = await GetValidatedClientAsync(
            request.ClientId ?? tokenEntity.Authorization.Client.ClientId,
            request.ClientSecret,
            missingDescription: "Missing client credentials"
        );

        if (client.Id != tokenEntity.Authorization.ClientId)
        {
            throw new BusinessException(Localizer.OAuthClientMismatch);
        }

        // Get user
        var user = await GetUserWithRolesAsync(tokenEntity.SubjectId);
        if (user == null)
        {
            throw new BusinessException(Localizer.UserNotFound);
        }

        // Generate new tokens
        return await GenerateTokensAsync(
            user,
            client,
            tokenEntity.Authorization.Scopes,
            signingKey,
            tokenEntity.AuthorizationId,
            tokenEntity,
            ReadTokenProperties(tokenEntity).GetValueOrDefault("session_id")
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

        return audiences.Select(aud => new Claim(JwtRegisteredClaimNames.Aud, aud!));
    }

    private static IEnumerable<Claim> BuildStandardUserClaims(User user)
    {
        yield return new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString());
        yield return new Claim(JwtRegisteredClaimNames.Name, user.UserName);

        if (!string.IsNullOrEmpty(user.Email))
        {
            yield return new Claim(JwtRegisteredClaimNames.Email, user.Email);
        }
    }

    private static IEnumerable<Claim> BuildRoleClaims(IEnumerable<string> roles)
    {
        return roles.Select(role => new Claim(ClaimTypes.Role, role));
    }

    private async Task<User?> GetUserWithRolesAsync(string? subjectId)
    {
        if (string.IsNullOrWhiteSpace(subjectId) || !Guid.TryParse(subjectId, out var userId))
        {
            return null;
        }

        return await _dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);
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
            new(JwtRegisteredClaimNames.Sub, client.Id.ToString()),
            new(OpenIdConnectParameterNames.ClientId, client.ClientId),
            new(OpenIdConnectParameterNames.Scope, request.Scope ?? ""),
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

        if (!client.AllowPasswordGrant)
        {
            await WriteAuditAsync(
                category: "Authentication",
                eventName: "PasswordGrantRejected",
                subjectId: client.Id.ToString(),
                payload: JsonSerializer.Serialize(new
                {
                    client.ClientId,
                    client.PasswordGrantRestrictionReason,
                })
            );

            throw new BusinessException(Localizer.OAuthPasswordGrantDisabled, StatusCodes.Status400BadRequest);
        }

        var normalizedEmail = request.Username.Trim().ToUpperInvariant();

        // Find user by email only
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            _riskControlService.RegisterLoginFailure(normalizedEmail, user?.Id, null);
            throw new BusinessException(Localizer.InvalidEmailOrPassword);
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
        {
            throw new BusinessException(Localizer.LockAccountForManyTimes, StatusCodes.Status403Forbidden);
        }

        // Verify password
        var passwordValid = HashCrypto.Validate(request.Password, user.PasswordSalt, user.PasswordHash);
        if (!passwordValid)
        {
            _riskControlService.RegisterLoginFailure(normalizedEmail, user.Id, null);
            user.AccessFailedCount++;
            if (user.LockoutEnabled && user.AccessFailedCount >= _riskControlService.LoginFailureThreshold)
            {
                user.LockoutEnd = DateTimeOffset.UtcNow.Add(_riskControlService.AccountLockoutDuration);
            }

            user.UpdatedTime = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            await WriteAuditAsync(
                category: "Authentication",
                eventName: "PasswordGrantFailed",
                subjectId: user.Id.ToString(),
                payload: JsonSerializer.Serialize(new
                {
                    reason = "InvalidPassword",
                    failedCount = user.AccessFailedCount,
                    lockoutEnd = user.LockoutEnd,
                })
            );
            throw new BusinessException(Localizer.InvalidEmailOrPassword);
        }

        _riskControlService.ResetLoginFailures(normalizedEmail, user.Id, null);
        if (user.AccessFailedCount != 0)
        {
            user.AccessFailedCount = 0;
            user.UpdatedTime = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
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

        var devicePollingAssessment = _riskControlService.RegisterDeviceCodePoll(request.ClientId, request.DeviceCode);
        if (devicePollingAssessment.IsBlocked)
        {
            await WriteAuditAsync(
                category: "DeviceFlow",
                eventName: "DeviceCodePollingThrottled",
                payload: JsonSerializer.Serialize(new
                {
                    request.ClientId,
                    request.DeviceCode,
                    devicePollingAssessment.AttemptCount,
                    devicePollingAssessment.BlockedUntil,
                })
            );

            throw new BusinessException(Localizer.OAuthSlowDown, StatusCodes.Status429TooManyRequests);
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
        var user = await GetUserWithRolesAsync(tokenEntity.SubjectId);
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
        Guid? authorizationId = null,
        Token? rotatedRefreshToken = null,
        string? sessionId = null,
        string? nonce = null
    )
    {
        var roles = user.UserRoles
            .Select(ur => ur.Role?.Name)
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.Ordinal)
            .Cast<string>()
            .ToArray();

        // Build claims
        var claims = new List<Claim>
        {
            new(OpenIdConnectParameterNames.ClientId, client.ClientId),
        };

        claims.AddRange(BuildStandardUserClaims(user));

        claims.AddRange(BuildRoleClaims(roles));

        claims.AddRange(BuildAudienceClaims(client));

        if (!string.IsNullOrEmpty(scope))
        {
            claims.Add(new Claim(OpenIdConnectParameterNames.Scope, scope));
        }

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Sid, sessionId));
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
            Properties = SerializeTokenProperties(
                new Dictionary<string, string>()
                {
                    ["root_token"] = GetRootRefreshTokenReference(rotatedRefreshToken),
                    ["rotated_from"] = rotatedRefreshToken?.ReferenceId ?? string.Empty,
                    ["session_id"] = sessionId ?? string.Empty,
                }
            ),
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(30),
        };

        if (rotatedRefreshToken != null)
        {
            var rotatedProperties = ReadTokenProperties(rotatedRefreshToken);
            rotatedProperties["rotated_to"] = refreshTokenValue;
            rotatedProperties["rotated_at"] = DateTimeOffset.UtcNow.ToString("O");
            rotatedRefreshToken.Status = TokenStatuses.Redeemed;
            rotatedRefreshToken.RedemptionDate = DateTimeOffset.UtcNow;
            rotatedRefreshToken.Properties = SerializeTokenProperties(rotatedProperties);
        }

        await _dbContext.Tokens.AddAsync(accessTokenEntity);
        await _dbContext.Tokens.AddAsync(refreshTokenEntity);
        await _dbContext.SaveChangesAsync();

        if (rotatedRefreshToken != null)
        {
            await WriteAuditAsync(
                category: "Authentication",
                eventName: "RefreshTokenRotated",
                subjectId: user.Id.ToString(),
                payload: JsonSerializer.Serialize(
                    new
                    {
                        authorizationId,
                        previousRefreshToken = rotatedRefreshToken.ReferenceId,
                        newRefreshToken = refreshTokenValue,
                    }
                )
            );
        }

        // Generate ID token if openid scope is present
        string? idToken = null;
        if (HasScope(scope, Scopes.OpenId))
        {
            var idClaims = new List<Claim>(BuildStandardUserClaims(user));

            idClaims.AddRange(BuildRoleClaims(roles));

            idClaims.Add(new Claim(JwtRegisteredClaimNames.Aud, client.ClientId));

            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                idClaims.Add(new Claim(JwtRegisteredClaimNames.Sid, sessionId));
            }

            if (!string.IsNullOrWhiteSpace(nonce))
            {
                idClaims.Add(new Claim(JwtRegisteredClaimNames.Nonce, nonce));
            }

            idToken = _oauthService.GenerateToken(
                idClaims,
                signingKey,
                3600,
                includeDefaultAudience: false
            );
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
        if (!string.IsNullOrEmpty(client.SecretHash))
        {
            if (string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(client.SecretSalt))
            {
                return null;
            }

            var secretValid = HashCrypto.Validate(clientSecret, client.SecretSalt, client.SecretHash);
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
    public async Task<bool> RevokeTokenAsync(string token, string? tokenTypeHint, Guid requestingClientId)
    {
        var tokenEntity = await _dbContext.Tokens
            .Include(t => t.Authorization)
            .ThenInclude(a => a!.Client)
            .FirstOrDefaultAsync(t => t.ReferenceId == token || t.Payload == token);

        if (tokenEntity == null)
        {
            return true; // Token doesn't exist, consider it revoked
        }

        if (!CanClientManageToken(tokenEntity, requestingClientId))
        {
            await WriteAuditAsync(
                category: "Authentication",
                eventName: "TokenRevocationDenied",
                subjectId: requestingClientId.ToString(),
                payload: JsonSerializer.Serialize(new { tokenTypeHint, tokenType = tokenEntity.Type })
            );
            return true;
        }

        tokenEntity.Status = TokenStatuses.Revoked;
        await _dbContext.SaveChangesAsync();

        await WriteAuditAsync(
            category: "Authentication",
            eventName: "TokenRevoked",
            subjectId: tokenEntity.SubjectId ?? requestingClientId.ToString(),
            payload: JsonSerializer.Serialize(
                new
                {
                    tokenTypeHint,
                    tokenType = tokenEntity.Type,
                    requestingClientId,
                }
            )
        );

        return true;
    }

    public async Task<int> RevokeAuthorizationChainAsync(
        Guid userId,
        string? sessionId = null,
        string? ipAddress = null,
        string? userAgent = null
    )
    {
        var activeRefreshTokens = await _dbContext
            .Tokens.Where(t =>
                t.SubjectId == userId.ToString()
                && t.Type == TokenTypes.RefreshToken
                && t.Status == TokenStatuses.Valid
            )
            .ToListAsync();

        if (activeRefreshTokens.Count == 0)
        {
            return 0;
        }

        foreach (var token in activeRefreshTokens)
        {
            token.Status = TokenStatuses.Revoked;
            var properties = ReadTokenProperties(token);
            properties["revoked_at"] = DateTimeOffset.UtcNow.ToString("O");
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                properties["revoked_session_id"] = sessionId;
            }

            token.Properties = SerializeTokenProperties(properties);
        }

        await _dbContext.SaveChangesAsync();
        await WriteAuditAsync(
            category: "Authentication",
            eventName: "AuthorizationChainRevoked",
            subjectId: userId.ToString(),
            payload: JsonSerializer.Serialize(new { count = activeRefreshTokens.Count, sessionId }),
            ipAddress: ipAddress,
            userAgent: userAgent
        );

        return activeRefreshTokens.Count;
    }

    /// <summary>
    /// Introspect token
    /// </summary>
    public async Task<IntrospectResponseDto> IntrospectTokenAsync(
        string token,
        string? tokenTypeHint,
        Guid requestingClientId
    )
    {
        var tokenEntity = await _dbContext
            .Tokens.Include(t => t.Authorization)
            .ThenInclude(a => a!.Client)
            .FirstOrDefaultAsync(t => t.ReferenceId == token || t.Payload == token);

        if (tokenEntity == null || tokenEntity.Status != TokenStatuses.Valid)
        {
            await WriteAuditAsync(
                category: "Authentication",
                eventName: "TokenIntrospectionMiss",
                subjectId: requestingClientId.ToString(),
                payload: JsonSerializer.Serialize(new { tokenTypeHint })
            );
            return new IntrospectResponseDto { Active = false };
        }

        if (!CanClientManageToken(tokenEntity, requestingClientId))
        {
            await WriteAuditAsync(
                category: "Authentication",
                eventName: "TokenIntrospectionDenied",
                subjectId: requestingClientId.ToString(),
                payload: JsonSerializer.Serialize(new { tokenTypeHint, tokenType = tokenEntity.Type })
            );
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

        await WriteAuditAsync(
            category: "Authentication",
            eventName: "TokenIntrospected",
            subjectId: tokenEntity.SubjectId ?? requestingClientId.ToString(),
            payload: JsonSerializer.Serialize(
                new
                {
                    tokenTypeHint,
                    tokenType = tokenEntity.Type,
                    requestingClientId,
                }
            )
        );

        return response;
    }

    private async Task HandleRefreshTokenReuseAsync(Token reusedToken)
    {
        var now = DateTimeOffset.UtcNow;
        var properties = ReadTokenProperties(reusedToken);
        properties["reuse_detected_at"] = now.ToString("O");
        properties["compromised"] = bool.TrueString;
        reusedToken.Properties = SerializeTokenProperties(properties);
        reusedToken.Status = TokenStatuses.Revoked;

        if (reusedToken.AuthorizationId.HasValue)
        {
            var relatedTokens = await _dbContext
                .Tokens.Where(t =>
                    t.AuthorizationId == reusedToken.AuthorizationId
                    && t.Type == TokenTypes.RefreshToken
                    && t.Status == TokenStatuses.Valid
                )
                .ToListAsync();

            foreach (var token in relatedTokens)
            {
                var tokenProperties = ReadTokenProperties(token);
                tokenProperties["revoked_reason"] = "refresh_token_reuse";
                tokenProperties["revoked_at"] = now.ToString("O");
                token.Properties = SerializeTokenProperties(tokenProperties);
                token.Status = TokenStatuses.Revoked;
            }

            var authorization = await _dbContext.Authorizations.FirstOrDefaultAsync(a =>
                a.Id == reusedToken.AuthorizationId.Value
            );
            if (authorization != null)
            {
                authorization.Status = AuthorizationStatuses.Revoked;
            }
        }

        await _dbContext.SaveChangesAsync();

        await WriteAuditAsync(
            category: "Authentication",
            eventName: "RefreshTokenReuseDetected",
            subjectId: reusedToken.SubjectId,
            payload: JsonSerializer.Serialize(
                new
                {
                    authorizationId = reusedToken.AuthorizationId,
                    refreshToken = reusedToken.ReferenceId,
                }
            )
        );
    }

    private static Dictionary<string, string> ReadTokenProperties(Token token)
    {
        if (string.IsNullOrWhiteSpace(token.Properties))
        {
            return [];
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(token.Properties) ?? [];
    }

    private static string SerializeTokenProperties(Dictionary<string, string> properties)
    {
        var sanitized = properties
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Value))
            .ToDictionary(static pair => pair.Key, static pair => pair.Value);

        return JsonSerializer.Serialize(sanitized);
    }

    private static bool HasScope(string? scopes, string requiredScope)
    {
        if (string.IsNullOrWhiteSpace(scopes) || string.IsNullOrWhiteSpace(requiredScope))
        {
            return false;
        }

        return scopes
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(requiredScope, StringComparer.Ordinal);
    }

    private static string GetRootRefreshTokenReference(Token? rotatedRefreshToken)
    {
        if (rotatedRefreshToken == null)
        {
            return string.Empty;
        }

        var properties = ReadTokenProperties(rotatedRefreshToken);
        return properties.GetValueOrDefault("root_token")
            ?? rotatedRefreshToken.ReferenceId
            ?? string.Empty;
    }

    private Task WriteAuditAsync(
        string category,
        string eventName,
        string? subjectId = null,
        string? payload = null,
        string? ipAddress = null,
        string? userAgent = null
    )
    {
        if (_auditLogManager == null)
        {
            return Task.CompletedTask;
        }

        return _auditLogManager.AddAuditLogAsync(
            category,
            eventName,
            subjectId,
            payload,
            ipAddress,
            userAgent
        );
    }

    private static bool CanClientManageToken(Token tokenEntity, Guid requestingClientId)
    {
        if (tokenEntity.Authorization?.ClientId == requestingClientId)
        {
            return true;
        }

        return Guid.TryParse(tokenEntity.SubjectId, out var subjectId)
            && subjectId == requestingClientId;
    }
}
