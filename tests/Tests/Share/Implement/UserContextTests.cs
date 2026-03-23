using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using Share.Constants;
using Share.Implement;
using ClaimTypes = System.Security.Claims.ClaimTypes;

namespace Tests.Share.Implement;

public class UserContextTests
{
    private const string AdminUserRole = "AdminUser";

    [Fact]
    public void Constructor_ShouldUseDotNetClaims_WhenAvailable()
    {
        var userId = Guid.CreateVersion7();
        var httpContextAccessor = CreateHttpContextAccessor(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "alice"),
            new Claim(ClaimTypes.Email, "alice@example.com"),
            new Claim(ClaimTypes.Role, AdminUserRole),
        ]);

        var userContext = new UserContext(httpContextAccessor);

        Assert.Equal(userId, userContext.UserId);
        Assert.Equal("alice", userContext.UserName);
        Assert.Equal("alice@example.com", userContext.Email);
        Assert.True(userContext.IsAdmin);
        Assert.Contains(AdminUserRole, userContext.Roles ?? []);
    }

    [Fact]
    public void Constructor_ShouldFallbackToOidcClaims_WhenDotNetClaimsMissing()
    {
        var userId = Guid.CreateVersion7();
        var httpContextAccessor = CreateHttpContextAccessor(
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, "oidc-user"),
            new Claim(JwtRegisteredClaimNames.Email, "oidc@example.com"),
            new Claim(ClaimTypes.Role, "Operator"),
        ]);

        var userContext = new UserContext(httpContextAccessor);

        Assert.Equal(userId, userContext.UserId);
        Assert.Equal("oidc-user", userContext.UserName);
        Assert.Equal("oidc@example.com", userContext.Email);
        Assert.False(userContext.IsAdmin);
        Assert.Equal(["Operator"], userContext.Roles);
    }

    [Fact]
    public void Constructor_ShouldSnapshotClaimsAtConstruction()
    {
        var httpContext = new DefaultHttpContext();
        var userId = Guid.CreateVersion7();
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, "early-user"),
                new Claim(ClaimTypes.Role, AdminUserRole),
            ],
            "Test")
        );

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = httpContext,
        };

        var userContext = new UserContext(httpContextAccessor);

        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, "late-user"),
                new Claim(ClaimTypes.Role, "Operator"),
            ],
            "Test")
        );

        Assert.Equal(userId, userContext.UserId);
        Assert.Equal("early-user", userContext.UserName);
        Assert.True(userContext.IsAdmin);
        Assert.Equal([AdminUserRole], userContext.Roles);
    }

    private static IHttpContextAccessor CreateHttpContextAccessor(IEnumerable<Claim> claims)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

        return new HttpContextAccessor
        {
            HttpContext = httpContext,
        };
    }
}