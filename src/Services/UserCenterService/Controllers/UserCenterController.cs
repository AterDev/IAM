using UserCenterMod.Managers;
using UserCenterMod.Models;

namespace UserCenterService.Controllers;

/// <summary>
/// User center APIs for password login and proxy traffic usage.
/// </summary>
public class UserCenterController(
    Localizer localizer,
    UserCenterManager manager
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

        var usage = await manager.AddProxyUsageAsync(
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

        return Ok(await manager.GetProxyUsagePageAsync(tokenUser.UserId, filter, cancellationToken));
    }

    private Task<UserCenterTokenUser?> GetTokenUserAsync(CancellationToken cancellationToken)
    {
        var authorization = Request.Headers.Authorization.ToString();
        return manager.ValidateTokenAsync(authorization, cancellationToken);
    }
}
