using UserCenterMod.Managers;
using UserCenterMod.Models;

namespace ApiService.Endpoints;

public sealed class UserCenterEndpoints : RestEndpointBase
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/UserCenter")
            .WithTags("UserCenter")
            .AllowAnonymous();
        group.MapPost("/login", Login).WithName("UserCenter_Login");
        group.MapPost("/proxyUsage", AddProxyUsage).WithName("UserCenter_AddProxyUsage");
        group.MapGet("/proxyUsage", GetProxyUsage).WithName("UserCenter_GetProxyUsage");
        group.MapGet("/GetEntitlement", GetEntitlement).WithName("UserCenter_GetEntitlement");
    }

    private static async Task<ApiResult<Guid>> Login(
        UserCenterLoginDto dto,
        UserCenterManager manager,
        CancellationToken cancellationToken) =>
        await manager.LoginAsync(dto, cancellationToken);

    private static async Task<ApiResult<long>> AddProxyUsage(
        HttpContext context,
        ProxyUsageAddDto dto,
        UserCenterManager manager,
        ProxyUsageManager proxyUsageManager,
        CancellationToken cancellationToken)
    {
        var tokenUser = await GetTokenUserAsync(context, manager, cancellationToken);
        if (tokenUser is null)
        {
            return TypedResults.Unauthorized();
        }
        return await proxyUsageManager.AddProxyUsageAsync(tokenUser.UserId, dto.Usage, cancellationToken);
    }

    private static async Task<ApiResult<PageList<ProxyUsageItemDto>>> GetProxyUsage(
        HttpContext context,
        [AsParameters] ProxyUsageFilterDto filter,
        int? pageIndex,
        int? pageSize,
        string? orderBy,
        UserCenterManager manager,
        ProxyUsageManager proxyUsageManager,
        CancellationToken cancellationToken)
    {
        var tokenUser = await GetTokenUserAsync(context, manager, cancellationToken);
        if (tokenUser is null)
        {
            return TypedResults.Unauthorized();
        }
        return await proxyUsageManager.GetProxyUsagePageAsync(tokenUser.UserId, FilterQuery.Apply(filter, pageIndex, pageSize, orderBy), cancellationToken);
    }

    private static async Task<ApiResult<UserEntitlementDetailDto>> GetEntitlement(
        HttpContext context,
        string entitlementCode,
        UserCenterManager manager,
        UserEntitlementManager userEntitlementManager,
        CancellationToken cancellationToken)
    {
        var tokenUser = await GetTokenUserAsync(context, manager, cancellationToken);
        if (tokenUser is null)
        {
            return TypedResults.Unauthorized();
        }
        if (string.IsNullOrWhiteSpace(entitlementCode))
        {
            return BadRequest(Localizer.BadRequest);
        }
        var entitlement = await userEntitlementManager.GetActiveEntitlementAsync(
            tokenUser.UserId, entitlementCode, cancellationToken);
        return entitlement is null ? Forbid("Entitlement is unavailable.") : entitlement;
    }

    private static Task<UserCenterTokenUser?> GetTokenUserAsync(
        HttpContext context,
        UserCenterManager manager,
        CancellationToken cancellationToken)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        return manager.ValidateTokenAsync(authorization, cancellationToken);
    }
}
