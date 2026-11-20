using IAMMod.Managers;
using IAMMod.Models.OAuthDtos;
using Microsoft.AspNetCore.Cors;

namespace ApiService.Endpoints;

public sealed class DiscoveryEndpoints : RestEndpointBase
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/.well-known")
            .WithTags("Discovery")
            .RequireCors(AppConst.Default)
            .AllowAnonymous();
        group.MapGet("/openid-configuration", GetConfiguration).WithName("Discovery_GetConfiguration");
        group.MapGet("/jwks", GetJwks).WithName("Discovery_GetJwks");
    }

    private static async Task<IResult> GetConfiguration(
        HttpContext context,
        DiscoveryManager discoveryManager,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ILogger<DiscoveryEndpoints> logger)
    {
        try
        {
            var issuer = ResolveIssuer(context, configuration, hostEnvironment, logger);
            return TypedResults.Ok(await discoveryManager.GetConfigurationAsync(issuer));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to generate OIDC configuration");
            return TypedResults.Problem("Failed to generate configuration", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> GetJwks(
        SigningKeyManager signingKeyManager,
        DiscoveryManager discoveryManager,
        ILogger<DiscoveryEndpoints> logger)
    {
        try
        {
            var validKeys = await signingKeyManager.GetValidPublicKeysAsync();
            return TypedResults.Ok(await discoveryManager.GetJwksAsync(validKeys));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to generate JWKS");
            return TypedResults.Problem("Failed to generate JWKS", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static string ResolveIssuer(
        HttpContext context,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ILogger<DiscoveryEndpoints> logger)
    {
        var configuredIssuer = configuration["Authentication:Issuer"]
            ?? configuration["Authentication:Jwt:ValidIssuer"];
        if (Uri.TryCreate(configuredIssuer, UriKind.Absolute, out var issuerUri))
        {
            return issuerUri.ToString().TrimEnd('/');
        }

        var requestIssuer = $"{context.Request.Scheme}://{context.Request.Host}";
        if (hostEnvironment.IsProduction())
        {
            logger.LogWarning(
                "Issuer configuration is missing or invalid in production. Falling back to request issuer {Issuer}.",
                requestIssuer);
        }
        return requestIssuer;
    }
}
