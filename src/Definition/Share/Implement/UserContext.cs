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
        HttpContext = httpContextAccessor?.HttpContext;
        var principal = HttpContext?.User;

        if (Guid.TryParse(
            principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal?.FindFirstValue(JwtRegisteredClaimNames.Sub),
            out var userId)
            && userId != Guid.Empty)
        {
            UserId = userId;
        }

        if (Guid.TryParse(principal?.FindFirstValue(ClaimTypes.GroupSid), out var groupId)
            && groupId != Guid.Empty)
        {
            GroupId = groupId;
        }

        if (Guid.TryParse(principal?.FindFirstValue(CustomClaimTypes.TenantId), out var tenantId)
            && tenantId != Guid.Empty)
        {
            TenantId = tenantId;
        }

        UserName = principal?.FindFirstValue(ClaimTypes.Name)
            ?? principal?.FindFirstValue(JwtRegisteredClaimNames.Name);
        Email = principal?.FindFirstValue(ClaimTypes.Email)
            ?? principal?.FindFirstValue(JwtRegisteredClaimNames.Email);
        CurrentRole = principal?.FindFirstValue(ClaimTypes.Role);
        Roles = principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        if (Roles != null)
        {
            IsAdmin = Roles.Any(r => r.Equals(WebConst.AdminUser) || r.Equals(WebConst.SuperAdmin));
        }
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
