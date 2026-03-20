using Microsoft.AspNetCore.Http;
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
            new Claim(OAuthConst.ClaimTypes.Subject, userId.ToString()),
            new Claim(OAuthConst.ClaimTypes.Name, "oidc-user"),
            new Claim(OAuthConst.ClaimTypes.Email, "oidc@example.com"),
            new Claim(ClaimTypes.Role, "Operator"),
        ]);

        var userContext = new UserContext(httpContextAccessor);

        Assert.Equal(userId, userContext.UserId);
        Assert.Equal("oidc-user", userContext.UserName);
        Assert.Equal("oidc@example.com", userContext.Email);
        Assert.False(userContext.IsAdmin);
        Assert.Equal(["Operator"], userContext.Roles);
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