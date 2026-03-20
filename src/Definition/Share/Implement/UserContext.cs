using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Perigon.AspNetCore.Abstraction;
using Perigon.AspNetCore.Constants;

namespace Share.Implement;

public class UserContext : IUserContext
{
    public Guid UserId { get; init; }

    public Guid? GroupId { get; init; }

    public Guid TenantId { get; init; }

    public string? UserName { get; init; }
    public string? Email { get; set; }

    public bool IsAdmin { get; init; }
    public string? CurrentRole { get; set; }
    public List<string>? Roles { get; set; }
    IReadOnlyList<string>? IUserContext.Roles => Roles;

    public HttpContext? HttpContext { get; set; }

    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        HttpContext = httpContextAccessor!.HttpContext;
        if (
            Guid.TryParse(FindClaimValue(ClaimTypes.NameIdentifier, JwtRegisteredClaimNames.Sub), out Guid userId)
            && userId != Guid.Empty
        )
        {
            UserId = userId;
        }
        if (
            Guid.TryParse(FindClaim(ClaimTypes.GroupSid)?.Value, out Guid groupSid)
            && groupSid != Guid.Empty
        )
        {
            GroupId = groupSid;
        }

        if (
            Guid.TryParse(FindClaim(CustomClaimTypes.TenantId)?.Value, out Guid tenantId)
            && tenantId != Guid.Empty
        )
        {
            TenantId = tenantId;
        }

        UserName = FindClaimValue(ClaimTypes.Name, JwtRegisteredClaimNames.Name);
        Email = FindClaimValue(ClaimTypes.Email, JwtRegisteredClaimNames.Email);

        CurrentRole = FindClaim(ClaimTypes.Role)?.Value;

        Roles = HttpContext?.User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        if (Roles != null)
        {
            IsAdmin = Roles.Any(r => r.Equals(WebConst.AdminUser) || r.Equals(WebConst.SuperAdmin));
        }
    }

    protected Claim? FindClaim(string claimType)
    {
        return HttpContext?.User?.FindFirst(claimType);
    }

    protected string? FindClaimValue(params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = FindClaim(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>
    /// 判断当前角色
    /// </summary>
    /// <param name="roleName"></param>
    /// <returns></returns>
    public bool IsRole(string roleName)
    {
        return Roles != null && Roles.Any(r => r == roleName);
    }

}
