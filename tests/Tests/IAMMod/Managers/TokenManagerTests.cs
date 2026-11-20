using System.Text;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;

namespace Tests.IAMMod.Managers;

public class TokenManagerTests
{
    [Fact]
    public async Task ProcessTokenRequestAsync_WhenPasswordGrantRequested_ThrowsBusinessException()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(ProcessTokenRequestAsync_WhenPasswordGrantRequested_ThrowsBusinessException));
        var client = SeedClient(dbContext, "password-disabled-client");
        dbContext.Users.Add(new User
        {
            UserName = "alice",
            NormalizedUserName = "ALICE",
            Email = "alice@example.com",
            NormalizedEmail = "ALICE@EXAMPLE.COM",
            PasswordSalt = "salt",
            PasswordHash = HashCrypto.GeneratePwd("P@ssw0rd!", "salt"),
            LockoutEnabled = true,
        });
        await dbContext.SaveChangesAsync();

        var manager = CreateTokenManager(dbContext);
        var signingKey = CreateSigningKey();

        var exception = await Assert.ThrowsAsync<BusinessException>(() => manager.ProcessTokenRequestAsync(
            new TokenRequestDto
            {
                GrantType = OAuthConst.GrantTypes.Password,
                ClientId = client.ClientId,
                Username = "alice@example.com",
                Password = "P@ssw0rd!",
            },
            signingKey));

        Assert.Equal(Localizer.OAuthPasswordGrantDisabled, exception.LanguageKey);
    }

    [Fact]
    public async Task ProcessTokenRequestAsync_WhenDeviceCodeClientSecretIsInvalid_ThrowsInvalidClient()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(ProcessTokenRequestAsync_WhenDeviceCodeClientSecretIsInvalid_ThrowsInvalidClient));
        var (client, _, token, _) = SeedDeviceGrant(
            dbContext,
            "device-confidential-client",
            clientType: ClientType.Confidential,
            clientSecret: "correct-secret");
        var manager = CreateTokenManager(dbContext);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => manager.ProcessTokenRequestAsync(
            new TokenRequestDto
            {
                GrantType = OAuthConst.GrantTypes.DeviceCode,
                ClientId = client.ClientId,
                ClientSecret = "wrong-secret",
                DeviceCode = token.ReferenceId,
            },
            CreateSigningKey()));

        Assert.Equal(Localizer.OAuthInvalidClient, exception.LanguageKey);
    }

    [Fact]
    public async Task ProcessTokenRequestAsync_WhenDeviceCodeBelongsToAnotherClient_ThrowsClientMismatch()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(ProcessTokenRequestAsync_WhenDeviceCodeBelongsToAnotherClient_ThrowsClientMismatch));
        var (ownerClient, _, token, _) = SeedDeviceGrant(dbContext, "device-owner-client");
        var requestingClient = SeedClient(dbContext, "device-requesting-client");
        var manager = CreateTokenManager(dbContext);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => manager.ProcessTokenRequestAsync(
            new TokenRequestDto
            {
                GrantType = OAuthConst.GrantTypes.DeviceCode,
                ClientId = requestingClient.ClientId,
                DeviceCode = token.ReferenceId,
            },
            CreateSigningKey()));

        Assert.Equal(Localizer.OAuthClientMismatch, exception.LanguageKey);
        Assert.Equal(ownerClient.Id, token.Authorization!.ClientId);
    }

    [Fact]
    public async Task ProcessTokenRequestAsync_WhenDeviceCodeIsExpiredWhilePending_ThrowsExpiredToken()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(ProcessTokenRequestAsync_WhenDeviceCodeIsExpiredWhilePending_ThrowsExpiredToken));
        var (client, _, token, _) = SeedDeviceGrant(
            dbContext,
            "device-expired-client",
            tokenStatus: OAuthConst.TokenStatuses.Pending,
            authorizationStatus: OAuthConst.AuthorizationStatuses.Pending,
            tokenExpiration: DateTimeOffset.UtcNow.AddMinutes(-1));
        var manager = CreateTokenManager(dbContext);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => manager.ProcessTokenRequestAsync(
            new TokenRequestDto
            {
                GrantType = OAuthConst.GrantTypes.DeviceCode,
                ClientId = client.ClientId,
                DeviceCode = token.ReferenceId,
            },
            CreateSigningKey()));

        Assert.Equal(Localizer.OAuthDeviceCodeExpired, exception.LanguageKey);
    }

    [Fact]
    public async Task ProcessTokenRequestAsync_WhenDeviceCodeExpirationIsMissing_ThrowsExpiredToken()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(ProcessTokenRequestAsync_WhenDeviceCodeExpirationIsMissing_ThrowsExpiredToken));
        var (client, _, token, _) = SeedDeviceGrant(dbContext, "device-missing-expiration-client");
        await dbContext.Tokens
            .Where(item => item.Id == token.Id)
            .ExecuteUpdateAsync(update => update.Set(item => item.ExpirationDate, null));
        var manager = CreateTokenManager(dbContext);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => manager.ProcessTokenRequestAsync(
            new TokenRequestDto
            {
                GrantType = OAuthConst.GrantTypes.DeviceCode,
                ClientId = client.ClientId,
                DeviceCode = token.ReferenceId,
            },
            CreateSigningKey()));

        Assert.Equal(Localizer.OAuthDeviceCodeExpired, exception.LanguageKey);
    }

    [Fact]
    public async Task ProcessTokenRequestAsync_WhenDeviceCodeWasRedeemed_ThrowsInvalidDeviceCode()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(ProcessTokenRequestAsync_WhenDeviceCodeWasRedeemed_ThrowsInvalidDeviceCode));
        var (client, _, token, _) = SeedDeviceGrant(
            dbContext,
            "device-redeemed-client",
            tokenStatus: OAuthConst.TokenStatuses.Redeemed);
        var manager = CreateTokenManager(dbContext);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => manager.ProcessTokenRequestAsync(
            new TokenRequestDto
            {
                GrantType = OAuthConst.GrantTypes.DeviceCode,
                ClientId = client.ClientId,
                DeviceCode = token.ReferenceId,
            },
            CreateSigningKey()));

        Assert.Equal(Localizer.OAuthInvalidDeviceCode, exception.LanguageKey);
    }

    [Fact]
    public async Task ProcessTokenRequestAsync_WhenAuthorizationIsNotAuthorized_ThrowsInvalidDeviceCode()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(ProcessTokenRequestAsync_WhenAuthorizationIsNotAuthorized_ThrowsInvalidDeviceCode));
        var (client, authorization, token, _) = SeedDeviceGrant(
            dbContext,
            "device-revoked-authorization-client",
            authorizationStatus: OAuthConst.AuthorizationStatuses.Revoked);
        var manager = CreateTokenManager(dbContext);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => manager.ProcessTokenRequestAsync(
            new TokenRequestDto
            {
                GrantType = OAuthConst.GrantTypes.DeviceCode,
                ClientId = client.ClientId,
                DeviceCode = token.ReferenceId,
            },
            CreateSigningKey()));

        Assert.Equal(Localizer.OAuthInvalidDeviceCode, exception.LanguageKey);
        Assert.Equal(OAuthConst.AuthorizationStatuses.Revoked, authorization.Status);
    }

    [Fact]
    public async Task ProcessTokenRequestAsync_WhenUserIsLockedOut_RejectsDeviceCodeGrant()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(ProcessTokenRequestAsync_WhenUserIsLockedOut_RejectsDeviceCodeGrant));
        var (client, _, token, user) = SeedDeviceGrant(
            dbContext,
            "device-locked-user-client",
            lockoutEnd: DateTimeOffset.UtcNow.AddMinutes(10));
        var manager = CreateTokenManager(dbContext);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => manager.ProcessTokenRequestAsync(
            new TokenRequestDto
            {
                GrantType = OAuthConst.GrantTypes.DeviceCode,
                ClientId = client.ClientId,
                DeviceCode = token.ReferenceId,
            },
            CreateSigningKey()));

        Assert.Equal(Localizer.LockAccountForManyTimes, exception.LanguageKey);
        Assert.Equal(StatusCodes.Status403Forbidden, exception.StatusCodes);
        Assert.Equal(user.Id.ToString(), token.SubjectId);
        Assert.Equal(OAuthConst.TokenStatuses.Valid, token.Status);
    }

    [Fact]
    public async Task ProcessTokenRequestAsync_WhenDeviceCodeIsValid_ExchangesOnceAndRedeemsCode()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(ProcessTokenRequestAsync_WhenDeviceCodeIsValid_ExchangesOnceAndRedeemsCode));
        var (client, _, token, _) = SeedDeviceGrant(dbContext, "device-valid-client");
        var manager = CreateTokenManager(dbContext);

        var response = await manager.ProcessTokenRequestAsync(
            new TokenRequestDto
            {
                GrantType = OAuthConst.GrantTypes.DeviceCode,
                ClientId = client.ClientId,
                DeviceCode = token.ReferenceId,
            },
            CreateSigningKey());

        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
        var storedToken = await dbContext.Tokens
            .Where(item => item.Id == token.Id)
            .FirstOrDefaultAsync();
        Assert.NotNull(storedToken);
        Assert.Equal(OAuthConst.TokenStatuses.Redeemed, storedToken.Status);
        Assert.NotNull(storedToken.RedemptionDate);
    }

    [Fact]
    public async Task ProcessTokenRequestAsync_OnRelationalStore_AllowsOnlyOneDeviceCodeRedemption()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(ProcessTokenRequestAsync_OnRelationalStore_AllowsOnlyOneDeviceCodeRedemption));
        var (client, _, token, _) = SeedDeviceGrant(dbContext, "device-sqlite-client");
        var manager = CreateTokenManager(dbContext);
        var request = new TokenRequestDto
        {
            GrantType = OAuthConst.GrantTypes.DeviceCode,
            ClientId = client.ClientId,
            DeviceCode = token.ReferenceId,
        };

        var response = await manager.ProcessTokenRequestAsync(request, CreateSigningKey());
        var exception = await Assert.ThrowsAsync<BusinessException>(() => manager.ProcessTokenRequestAsync(request, CreateSigningKey()));

        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.Equal(Localizer.OAuthInvalidDeviceCode, exception.LanguageKey);
    }

    [Fact]
    public async Task IntrospectTokenAsync_WhenClientOwnsToken_ReturnsActive()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(IntrospectTokenAsync_WhenClientOwnsToken_ReturnsActive));
        var client = SeedClient(dbContext, "owner-client");
        var authorization = SeedAuthorization(dbContext, client, Guid.NewGuid().ToString());
        dbContext.Tokens.Add(new Token
        {
            AuthorizationId = authorization.Id,
            Authorization = authorization,
            ReferenceId = "access-token-1",
            Type = OAuthConst.TokenTypes.AccessToken,
            Status = OAuthConst.TokenStatuses.Valid,
            SubjectId = authorization.SubjectId,
            Payload = "payload-1",
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = DateTimeOffset.UtcNow.AddMinutes(30),
        });
        await dbContext.SaveChangesAsync();

        var manager = CreateTokenManager(dbContext);
        var response = await manager.IntrospectTokenAsync("access-token-1", OAuthConst.TokenTypes.AccessToken, client.Id);

        Assert.True(response.Active);
        Assert.Equal(client.ClientId, response.ClientId);
    }

    [Fact]
    public async Task IntrospectTokenAsync_WhenDifferentClientRequestsToken_ReturnsInactive()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(IntrospectTokenAsync_WhenDifferentClientRequestsToken_ReturnsInactive));
        var ownerClient = SeedClient(dbContext, "owner-client-introspect");
        var otherClient = SeedClient(dbContext, "other-client-introspect");
        var authorization = SeedAuthorization(dbContext, ownerClient, Guid.NewGuid().ToString());
        dbContext.Tokens.Add(new Token
        {
            AuthorizationId = authorization.Id,
            Authorization = authorization,
            ReferenceId = "access-token-2",
            Type = OAuthConst.TokenTypes.AccessToken,
            Status = OAuthConst.TokenStatuses.Valid,
            SubjectId = authorization.SubjectId,
            Payload = "payload-2",
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = DateTimeOffset.UtcNow.AddMinutes(30),
        });
        await dbContext.SaveChangesAsync();

        var manager = CreateTokenManager(dbContext);
        var response = await manager.IntrospectTokenAsync("access-token-2", OAuthConst.TokenTypes.AccessToken, otherClient.Id);

        Assert.False(response.Active);
    }

    [Fact]
    public async Task RevokeTokenAsync_WhenClientOwnsRefreshToken_RevokesToken()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(RevokeTokenAsync_WhenClientOwnsRefreshToken_RevokesToken));
        var client = SeedClient(dbContext, "owner-client-revoke");
        var authorization = SeedAuthorization(dbContext, client, Guid.NewGuid().ToString());
        var token = new Token
        {
            AuthorizationId = authorization.Id,
            Authorization = authorization,
            ReferenceId = "refresh-token-1",
            Type = OAuthConst.TokenTypes.RefreshToken,
            Status = OAuthConst.TokenStatuses.Valid,
            SubjectId = authorization.SubjectId,
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(7),
        };
        dbContext.Tokens.Add(token);
        await dbContext.SaveChangesAsync();

        var manager = CreateTokenManager(dbContext);
        var result = await manager.RevokeTokenAsync("refresh-token-1", OAuthConst.TokenTypes.RefreshToken, client.Id);

        Assert.True(result);
        var storedToken = await dbContext.Tokens
            .Where(item => item.Id == token.Id)
            .FirstOrDefaultAsync();
        Assert.NotNull(storedToken);
        Assert.Equal(OAuthConst.TokenStatuses.Revoked, storedToken.Status);
    }

    [Fact]
    public async Task RevokeTokenAsync_WhenDifferentClientRequestsToken_LeavesTokenValid()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(RevokeTokenAsync_WhenDifferentClientRequestsToken_LeavesTokenValid));
        var ownerClient = SeedClient(dbContext, "owner-client-revoke-other");
        var otherClient = SeedClient(dbContext, "other-client-revoke-other");
        var authorization = SeedAuthorization(dbContext, ownerClient, Guid.NewGuid().ToString());
        var token = new Token
        {
            AuthorizationId = authorization.Id,
            Authorization = authorization,
            ReferenceId = "refresh-token-2",
            Type = OAuthConst.TokenTypes.RefreshToken,
            Status = OAuthConst.TokenStatuses.Valid,
            SubjectId = authorization.SubjectId,
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(7),
        };
        dbContext.Tokens.Add(token);
        await dbContext.SaveChangesAsync();

        var manager = CreateTokenManager(dbContext);
        var result = await manager.RevokeTokenAsync("refresh-token-2", OAuthConst.TokenTypes.RefreshToken, otherClient.Id);

        Assert.True(result);
        Assert.Equal(OAuthConst.TokenStatuses.Valid, token.Status);
    }

    private static TokenManager CreateTokenManager(DefaultDbContext dbContext)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Issuer"] = "https://issuer.example.com",
            })
            .Build();

        var jwtOptions = Options.Create(new JwtOption
        {
            ValidAudiences = "iam-tests",
            Sign = "unused",
        });
        var oauthService = new OAuthService(NullLogger<OAuthService>.Instance, jwtOptions, configuration);
        var riskControlService = new RiskControlService(
            dbContext,
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new RiskControlOption()),
            NullLogger<RiskControlService>.Instance);
        return new TokenManager(dbContext, NullLogger<TokenManager>.Instance, oauthService, null!, riskControlService);
    }

    private static Client SeedClient(DefaultDbContext dbContext, string clientId)
    {
        var client = new Client
        {
            ClientId = clientId,
            DisplayName = clientId,
        };

        dbContext.Clients.Add(client);
        dbContext.SaveChanges();
        return client;
    }

    private static (Client Client, Authorization Authorization, Token Token, User User) SeedDeviceGrant(
        DefaultDbContext dbContext,
        string clientId,
        string tokenStatus = OAuthConst.TokenStatuses.Valid,
        string authorizationStatus = OAuthConst.AuthorizationStatuses.Authorized,
        DateTimeOffset? tokenExpiration = null,
        DateTimeOffset? authorizationExpiration = null,
        DateTimeOffset? lockoutEnd = null,
        ClientType? clientType = null,
        string? clientSecret = null)
    {
        var client = new Client
        {
            ClientId = clientId,
            DisplayName = clientId,
            Type = clientType,
            RegistrationStatus = ClientRegistrationStatus.Approved,
        };

        if (!string.IsNullOrWhiteSpace(clientSecret))
        {
            client.SecretSalt = HashCrypto.BuildSalt();
            client.SecretHash = HashCrypto.GeneratePwd(clientSecret, client.SecretSalt);
            client.SecretExpiresAt = DateTimeOffset.UtcNow.AddDays(1);
        }

        var user = new User
        {
            UserName = $"{clientId}-user",
            NormalizedUserName = $"{clientId}-user".ToUpperInvariant(),
            Email = $"{clientId}@example.com",
            NormalizedEmail = $"{clientId}@example.com".ToUpperInvariant(),
            PasswordSalt = "salt",
            PasswordHash = "hash",
            LockoutEnd = lockoutEnd,
        };

        dbContext.Clients.Add(client);
        dbContext.Users.Add(user);
        dbContext.SaveChanges();

        var authorization = new Authorization
        {
            SubjectId = user.Id.ToString(),
            ClientId = client.Id,
            Client = client,
            Type = OAuthConst.AuthorizationTypes.DeviceCode,
            Status = authorizationStatus,
            Scopes = "openid profile",
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = authorizationExpiration ?? DateTimeOffset.UtcNow.AddMinutes(10),
        };

        var token = new Token
        {
            AuthorizationId = authorization.Id,
            Authorization = authorization,
            ReferenceId = $"device-{clientId}",
            Type = OAuthConst.TokenTypes.DeviceCode,
            Status = tokenStatus,
            SubjectId = user.Id.ToString(),
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = tokenExpiration ?? DateTimeOffset.UtcNow.AddMinutes(5),
        };

        dbContext.Authorizations.Add(authorization);
        dbContext.Tokens.Add(token);
        dbContext.SaveChanges();

        return (client, authorization, token, user);
    }

    private static Authorization SeedAuthorization(DefaultDbContext dbContext, Client client, string subjectId)
    {
        var authorization = new Authorization
        {
            SubjectId = subjectId,
            ClientId = client.Id,
            Client = client,
            Type = OAuthConst.AuthorizationTypes.Code,
            Status = OAuthConst.AuthorizationStatuses.Valid,
            Scopes = "openid profile",
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(1),
        };

        dbContext.Authorizations.Add(authorization);
        dbContext.SaveChanges();
        return authorization;
    }

    private static SigningKey CreateSigningKey()
    {
        var (publicKey, privateKey) = HashCrypto.GenerateRsaKeyPair(2048);

        return new SigningKey
        {
            KeyId = Guid.NewGuid().ToString("N"),
            Algorithm = "RS256",
            KeyType = "RSA",
            PublicKey = publicKey,
            PrivateKey = privateKey,
            Usage = "signing",
            ActivationDate = DateTimeOffset.UtcNow.AddMinutes(-1),
            ExpirationDate = DateTimeOffset.UtcNow.AddDays(1),
            IsActive = true,
        };
    }

}
