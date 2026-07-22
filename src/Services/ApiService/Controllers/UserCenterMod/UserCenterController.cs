using Microsoft.AspNetCore.Authorization;
using UserCenterMod.Managers;
using UserCenterMod.Models;

namespace ApiService.Controllers.UserCenterMod;

/// <summary>
/// User center APIs for password login and proxy traffic usage.
/// </summary>
[AllowAnonymous]
public class UserCenterController(
    Localizer localizer,
    UserCenterManager manager,
    ProxyUsageManager proxyUsageManager,
    UserEntitlementManager userEntitlementManager
) : RestControllerBase(localizer)
{
    /// <summary>
    /// Password login.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<Guid>> Login(
        [FromBody] UserCenterLoginDto dto,
        CancellationToken cancellationToken
    )
    {
        return Ok(await manager.LoginAsync(dto, cancellationToken));
    }

    /// <summary>
    /// Add proxy usage for current user.
    /// </summary>
    [HttpPost("proxyUsage")]
    public async Task<ActionResult<long>> AddProxyUsage(
        [FromBody] ProxyUsageAddDto dto,
        CancellationToken cancellationToken
    )
    {
        var tokenUser = await GetTokenUserAsync(cancellationToken);
        if (tokenUser is null)
        {
            return Unauthorized();
        }

        var usage = await proxyUsageManager.AddProxyUsageAsync(
            tokenUser.UserId,
            dto.Usage,
            cancellationToken
        );
        return Ok(usage);
    }

    /// <summary>
    /// Query proxy usage for current user.
    /// </summary>
    [HttpGet("proxyUsage")]
    public async Task<ActionResult<PageList<ProxyUsageItemDto>>> GetProxyUsage(
        [FromQuery] ProxyUsageFilterDto filter,
        CancellationToken cancellationToken
    )
    {
        var tokenUser = await GetTokenUserAsync(cancellationToken);
        if (tokenUser is null)
        {
            return Unauthorized();
        }

        return Ok(await proxyUsageManager.GetProxyUsagePageAsync(tokenUser.UserId, filter, cancellationToken));
    }

    /// <summary>
    /// Get an active entitlement for the current user.
    /// </summary>
    [HttpGet("GetEntitlement")]
    public async Task<ActionResult<UserEntitlementDetailDto>> GetEntitlement(
        [FromQuery] string entitlementCode,
        CancellationToken cancellationToken)
    {
        var tokenUser = await GetTokenUserAsync(cancellationToken);
        if (tokenUser is null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(entitlementCode)) return BadRequest(Localizer.BadRequest);

        var entitlement = await userEntitlementManager.GetActiveEntitlementAsync(
            tokenUser.UserId, entitlementCode, cancellationToken);
        return entitlement is null ? Forbid("Entitlement is unavailable.") : Ok(entitlement);
    }

    private Task<UserCenterTokenUser?> GetTokenUserAsync(CancellationToken cancellationToken)
    {
        var authorization = Request.Headers.Authorization.ToString();
        return manager.ValidateTokenAsync(authorization, cancellationToken);
    }
}
