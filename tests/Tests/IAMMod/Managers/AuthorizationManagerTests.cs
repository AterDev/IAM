namespace Tests.IAMMod.Managers;

public class AuthorizationManagerTests
{
    [Fact]
    public async Task ValidateAuthorizationRequestAsync_WithUnsupportedScope_ReturnsInvalidScope()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(ValidateAuthorizationRequestAsync_WithUnsupportedScope_ReturnsInvalidScope));
        var scope = new ApiScope
        {
            Name = "api.read",
            DisplayName = "API Read",
            Description = "Read access",
        };
        var client = new Client
        {
            ClientId = "stage1-client",
            DisplayName = "Stage1 Client",
            RequirePkce = true,
            RedirectUris = ["https://app.example.com/callback"],
            ClientScopes =
            [
                new ClientScope
                {
                    Scope = scope,
                    ScopeId = scope.Id,
                }
            ],
        };

        dbContext.ApiScopes.Add(scope);
        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        var manager = new AuthorizationManager(dbContext, NullLogger<AuthorizationManager>.Instance);

        var result = await manager.ValidateAuthorizationRequestAsync(new AuthorizeRequestDto
        {
            ResponseType = OAuthConst.ResponseTypes.Code,
            ClientId = client.ClientId,
            RedirectUri = client.RedirectUris[0],
            Scope = "api.write",
            CodeChallenge = "challenge",
            CodeChallengeMethod = OAuthConst.CodeChallengeMethods.S256,
        });

        Assert.False(result.isValid);
        Assert.Equal(OAuthConst.ErrorCodes.InvalidScope, result.error);
    }

    [Fact]
    public async Task CreateAndValidateAuthorizationCodeAsync_WithPkce_RedeemsCode()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(CreateAndValidateAuthorizationCodeAsync_WithPkce_RedeemsCode));
        var client = new Client
        {
            ClientId = "pkce-client",
            DisplayName = "PKCE Client",
            RequirePkce = true,
            RedirectUris = ["https://app.example.com/callback"],
        };
        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        var manager = new AuthorizationManager(dbContext, NullLogger<AuthorizationManager>.Instance);
        var verifier = "abcdefghijklmnopqrstuvwxyz123456";
        var challenge = ToBase64Url(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(verifier)));

        var code = await manager.CreateAuthorizationCodeAsync(
            subjectId: Guid.NewGuid().ToString(),
            clientId: client.Id,
            redirectUri: client.RedirectUris[0],
            scope: "openid profile",
            codeChallenge: challenge,
            codeChallengeMethod: OAuthConst.CodeChallengeMethods.S256,
            nonce: "nonce-1",
            sessionId: "sid-1");

        var validation = await manager.ValidateAuthorizationCodeAsync(code, client.ClientId, client.RedirectUris[0], verifier);

        Assert.True(validation.isValid);
        Assert.NotNull(validation.authorization);

        var storedToken = await dbContext.Tokens.SingleAsync(t => t.ReferenceId == code);
        Assert.Equal(OAuthConst.TokenStatuses.Redeemed, storedToken.Status);
        Assert.NotNull(storedToken.RedemptionDate);
    }

    private static string ToBase64Url(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
