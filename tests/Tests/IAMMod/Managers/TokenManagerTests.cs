using System.Text;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;

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
        Assert.Equal(OAuthConst.TokenStatuses.Revoked, token.Status);
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
