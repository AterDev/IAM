using ApiService.Models;
using Perigon.PostgreSQL;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EntityFramework.AppDbContext;
using IAMMod.Managers;
using IAMMod.Models.AccountDtos;
using IAMMod.Models.AdminAuthDtos;
using IAMMod.Models.AuthorizationDtos;
using IAMMod.Models.ClientDtos;
using IAMMod.Models.LoginSessionDtos;
using IAMMod.Models.PermissionDtos;
using IAMMod.Models.UserDtos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using Perigon.AspNetCore.Services;
using Share.Exceptions;

namespace ApiService.Endpoints;

public sealed class AccountEndpoints : RestEndpointBase
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/Account").WithTags("Account");
        group.MapPost("/register", Register).WithName("Account_Register").AllowAnonymous();
        group.MapPost("/forgot-password", ForgotPassword).WithName("Account_ForgotPassword").AllowAnonymous();
        group.MapPost("/reset-password", ResetPassword).WithName("Account_ResetPassword").AllowAnonymous();
        var authenticated = group.MapGroup("").RequireAuthorization();
        authenticated.MapPost("/change-password", ChangePassword).WithName("Account_ChangePassword");
        authenticated.MapGet("/mfa", GetMfaStatus).WithName("Account_GetMfaStatus");
        authenticated.MapPost("/mfa/setup", BeginMfaSetup).WithName("Account_BeginMfaSetup");
        authenticated.MapPost("/mfa/enable", EnableMfa).WithName("Account_EnableMfa");
        authenticated.MapPost("/mfa/disable", DisableMfa).WithName("Account_DisableMfa");
        authenticated.MapPost("/mfa/recovery-codes/regenerate", RegenerateRecoveryCodes).WithName("Account_RegenerateRecoveryCodes");
    }

    private static async Task<ApiResult<UserDetailDto>> Register(RegisterRequestDto dto, UserManager manager, HttpContext context, ILogger<AccountEndpoints> logger)
    {
        try
        {
            var result = await manager.RegisterSelfServiceAsync(new UserAddDto
            {
                UserName = dto.UserName, Email = dto.Email, PhoneNumber = dto.PhoneNumber, Password = dto.Password,
                EmailConfirmed = false, PhoneNumberConfirmed = false, LockoutEnabled = true,
            }, context.Connection.RemoteIpAddress?.ToString(), context.Request.Headers.UserAgent.ToString());
            return result is null ? Problem(Localizer.BadRequest) : TypedResults.Created($"/api/Account/register/{result.Id}", result);
        }
        catch (BusinessException ex) when (ex.StatusCodes == StatusCodes.Status400BadRequest) { return Conflict(ex.LanguageKey); }
        catch (Exception ex) { logger.LogError(ex, "Error handling self-service registration"); return Problem(Localizer.BadRequest); }
    }

    private static async Task<ApiResult<ApiMessageDto>> ForgotPassword(ForgotPasswordRequestDto dto, UserManager manager, HttpContext context, ILogger<AccountEndpoints> logger)
    {
        try { await manager.RequestPasswordResetAsync(dto.Email, context.Connection.RemoteIpAddress?.ToString(), context.Request.Headers.UserAgent.ToString()); return new ApiMessageDto("Password reset request accepted"); }
        catch (Exception ex) { logger.LogError(ex, "Error handling forgot password request"); return Problem(Localizer.BadRequest); }
    }

    private static async Task<ApiResult<ApiMessageDto>> ResetPassword(ResetPasswordRequestDto dto, UserManager manager, HttpContext context, ILogger<AccountEndpoints> logger)
    {
        try { await manager.ResetPasswordAsync(dto.Email, dto.Code, dto.NewPassword, context.Connection.RemoteIpAddress?.ToString(), context.Request.Headers.UserAgent.ToString()); return new ApiMessageDto("Password reset completed"); }
        catch (BusinessException ex) when (ex.StatusCodes == StatusCodes.Status404NotFound) { return NotFound(ex.LanguageKey); }
        catch (BusinessException ex) { return BadRequest(ex.LanguageKey); }
        catch (Exception ex) { logger.LogError(ex, "Error resetting password"); return Problem(Localizer.BadRequest); }
    }

    private static async Task<ApiResult<ApiMessageDto>> ChangePassword(ChangePasswordRequestDto dto, UserManager manager, HttpContext context, ILogger<AccountEndpoints> logger, IUserContext user)
    {
        if (user.UserId == Guid.Empty) return TypedResults.Unauthorized();
        try { await manager.ChangeOwnPasswordAsync(user.UserId, dto.CurrentPassword, dto.NewPassword, context.Connection.RemoteIpAddress?.ToString(), context.Request.Headers.UserAgent.ToString()); return new ApiMessageDto("Password changed"); }
        catch (BusinessException ex) { logger.LogWarning(ex, "Password change rejected"); return BadRequest(ex.LanguageKey); }
    }

    private static async Task<ApiResult<MfaStatusDto>> GetMfaStatus(MfaManager manager, IUserContext user) => user.UserId == Guid.Empty ? TypedResults.Unauthorized() : await manager.GetStatusAsync(user.UserId);
    private static async Task<ApiResult<MfaSetupResponseDto>> BeginMfaSetup(MfaManager manager, IUserContext user, IConfiguration configuration, HttpContext context) => user.UserId == Guid.Empty ? TypedResults.Unauthorized() : await manager.BeginSetupAsync(user.UserId, ResolveIssuer(configuration, context));
    private static async Task<ApiResult<MfaRecoveryCodesResponseDto>> EnableMfa(EnableMfaRequestDto dto, MfaManager manager, IUserContext user, ILogger<AccountEndpoints> logger)
    { if (user.UserId == Guid.Empty) return TypedResults.Unauthorized(); try { return await manager.EnableAsync(user.UserId, dto.Code); } catch (BusinessException ex) { logger.LogWarning(ex, "MFA enable rejected"); return BadRequest(ex.LanguageKey); } }
    private static async Task<ApiResult<ApiMessageDto>> DisableMfa(DisableMfaRequestDto dto, MfaManager manager, IUserContext user, ILogger<AccountEndpoints> logger)
    { if (user.UserId == Guid.Empty) return TypedResults.Unauthorized(); try { await manager.DisableAsync(user.UserId, dto.Code); return new ApiMessageDto("MFA disabled"); } catch (BusinessException ex) { logger.LogWarning(ex, "MFA disable rejected"); return BadRequest(ex.LanguageKey); } }
    private static async Task<ApiResult<MfaRecoveryCodesResponseDto>> RegenerateRecoveryCodes(RegenerateRecoveryCodesRequestDto dto, MfaManager manager, IUserContext user, ILogger<AccountEndpoints> logger)
    { if (user.UserId == Guid.Empty) return TypedResults.Unauthorized(); try { return await manager.RegenerateRecoveryCodesAsync(user.UserId, dto.Code); } catch (BusinessException ex) { logger.LogWarning(ex, "MFA recovery code regeneration rejected"); return BadRequest(ex.LanguageKey); } }
    private static string ResolveIssuer(IConfiguration configuration, HttpContext context) => configuration["Authentication:DisplayName"]?.Trim() ?? configuration["Otel:Service:Name"]?.Trim() ?? (Uri.TryCreate(configuration["Authentication:Issuer"] ?? configuration["Authentication:Jwt:ValidIssuer"], UriKind.Absolute, out var uri) ? uri.Host : context.Request.Host.Host);
}

public sealed class AdminAuthEndpoints : RestEndpointBase
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = MapGroup(endpoints, "/api/admin", "AdminAuth");
        group.MapPost("/login", Login).WithName("AdminAuth_Login").AllowAnonymous();
        var auth = group.MapGroup("").RequireAuthorization();
        auth.MapGet("/me", GetCurrentUser).WithName("AdminAuth_GetCurrentUser");
        auth.MapPost("/logout", Logout).WithName("AdminAuth_Logout");
    }

    private static async Task<ApiResult<AdminLoginResponseDto>> Login(AdminLoginDto dto, UserManager users, RoleManager roles, DefaultDbContext db, SigningKeyManager keys, JwtService jwt, SessionManager sessions, HttpContext context, ILogger<AdminAuthEndpoints> logger)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString(); var agent = context.Request.Headers.UserAgent.ToString();
        var detail = await users.ValidateCredentialsAsync(dto.Email, dto.Password, ip, agent);
        if (detail is null) return Problem(Localizer.InvalidUserOrPassword);
        var user = await users.FindAsync(detail.Id); if (user is null) return Problem(Localizer.NotFoundUser);
        var roleNames = await roles.GetRoleNamesByIdsAsync(await db.UserRoles.Where(x => x.UserId == user.Id).Select(x => x.RoleId).ToScalarListAsync());
        var expires = jwt.ExpiredSecond; var sid = Guid.CreateVersion7().ToString(); var expiry = DateTimeOffset.UtcNow.AddSeconds(expires);
        await sessions.AddAsync(new LoginSessionAddDto { UserId = user.Id, SessionId = sid, IpAddress = ip, UserAgent = agent, DeviceInfo = agent, ExpirationTime = expiry }, ip, agent);
        jwt.Claims = [new Claim(ClaimTypes.Name, user.UserName), new Claim(ClaimTypes.Email, user.Email ?? string.Empty), new Claim(JwtRegisteredClaimNames.Sid, sid)];
        var signingKey = await keys.GetActiveSigningKeyAsync(); if (signingKey is null) { logger.LogError("No active signing key found for JWT generation."); return Problem("no active signing key found for JWT generation."); }
        using var rsa = HashCrypto.ImportRsaPrivateKey(signingKey.PrivateKey); var credentials = new SigningCredentials(new RsaSecurityKey(rsa) { KeyId = signingKey.KeyId }, SecurityAlgorithms.RsaSha256) { CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false } };
        return new AdminLoginResponseDto { AccessToken = jwt.GetToken(credentials, user.Id.ToString(), [.. roleNames]), TokenType = "Bearer", ExpiresIn = expires, SessionId = sid, User = new AdminUserInfo { Id = user.Id, UserName = user.UserName, Email = user.Email, Roles = roleNames } };
    }

    private static async Task<ApiResult<AdminUserInfo>> GetCurrentUser(UserManager users, RoleManager roles, DefaultDbContext db, IUserContext current)
    {
        if (current.UserId == Guid.Empty) return TypedResults.Unauthorized(); var user = await users.FindAsync(current.UserId); if (user is null) return NotFound("User not found");
        var names = await roles.GetRoleNamesByIdsAsync(await db.UserRoles.Where(x => x.UserId == user.Id).Select(x => x.RoleId).ToScalarListAsync());
        return new AdminUserInfo { Id = user.Id, UserName = user.UserName, Email = user.Email, Roles = names };
    }

    private static async Task<ApiResult<ApiMessageDto>> Logout(SessionManager sessions, IUserContext user, HttpContext context, ILogger<AdminAuthEndpoints> logger)
    {
        if (user.UserId == Guid.Empty) return TypedResults.Unauthorized(); var sid = context.User.FindFirst(JwtRegisteredClaimNames.Sid)?.Value; var ip = context.Connection.RemoteIpAddress?.ToString(); var agent = context.Request.Headers.UserAgent.ToString();
        if (!string.IsNullOrWhiteSpace(sid)) { var session = await sessions.GetBySessionIdAsync(sid); if (session is not null) await sessions.RevokeSessionAsync(session.Id, user.UserId.ToString(), ip, agent); } else await sessions.RevokeAllUserSessionsAsync(user.UserId, null, user.UserId.ToString(), ip, agent);
        await context.SignOutAsync(); return new ApiMessageDto("Logged out successfully");
    }
}

public sealed class ClientsEndpoints : RestEndpointBase
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = MapGroup(endpoints, "/api/Clients", "Clients", WebConst.AdminUser);
        group.MapGet("", GetPage).WithName("Clients_GetClients");
        group.MapGet("/{id:guid}", GetDetail).WithName("Clients_GetDetail");
        group.MapPost("", Create).WithName("Clients_CreateClient").RequireAuthorization(WebConst.AdminUser);
        group.MapPost("/register", Register).WithName("Clients_RegisterClient").RequireAuthorization();
        group.MapGet("/my-clients", GetMine).WithName("Clients_GetMyClients").RequireAuthorization();
        group.MapGet("/pending-registrations", GetPending).WithName("Clients_GetPendingRegistrations").RequireAuthorization(WebConst.AdminUser);
        group.MapPost("/{id:guid}/approve", Approve).WithName("Clients_ApproveClient").RequireAuthorization(WebConst.AdminUser);
        group.MapPut("/{id:guid}", Update).WithName("Clients_UpdateClient").RequireAuthorization(WebConst.AdminUser);
        group.MapDelete("/{id:guid}", Delete).WithName("Clients_DeleteClient").RequireAuthorization(WebConst.AdminUser);
        group.MapPost("/{id:guid}/secret:rotate", Rotate).WithName("Clients_RotateSecret").RequireAuthorization();
        group.MapGet("/{id:guid}/secrets", GetSecrets).WithName("Clients_GetSecrets").RequireAuthorization();
        group.MapPost("/{id:guid}/scopes", AssignScopes).WithName("Clients_AssignScopes").RequireAuthorization(WebConst.AdminUser);
        group.MapGet("/{id:guid}/authorizations", GetAuthorizations).WithName("Clients_GetAuthorizations").RequireAuthorization();
        group.MapPost("/{id:guid}/permissions", AssignPermissions).WithName("Clients_AssignPermissions").RequireAuthorization(WebConst.AdminUser);
        group.MapGet("/{id:guid}/permissions", GetPermissions).WithName("Clients_GetPermissions").RequireAuthorization(WebConst.AdminUser);
        group.MapGet("/{id:guid}/permission-tree", GetPermissionTree).WithName("Clients_GetPermissionTree").RequireAuthorization(WebConst.AdminUser);
        group.MapPost("/{id:guid}/menu-permissions:sync", SyncMenuPermissions).WithName("Clients_SyncMenuPermissions").RequireAuthorization(WebConst.AdminUser);
    }
    private static Task<PageList<ClientItemDto>> GetPage([AsParameters] ClientFilterDto filter, int? pageIndex, int? pageSize, string? orderBy, ClientManager manager) => manager.GetPageAsync(FilterQuery.Apply(filter, pageIndex, pageSize, orderBy));
    private static async Task<ApiResult<ClientDetailDto>> GetDetail(Guid id, ClientManager manager) => (await manager.GetDetailAsync(id)) is { } x ? x : NotFound("Client not found");
    private static Task<string?> Create(ClientAddDto dto, ClientManager manager) => manager.AddAsync(dto);
    private static async Task<ApiResult<ClientRegistrationResultDto>> Register(ClientRegistrationRequestDto dto, ClientManager manager) => await manager.RegisterAsync(dto);
    private static Task<List<ClientDetailDto>> GetMine(ClientManager manager) => manager.GetMyClientsAsync();
    private static Task<List<ClientDetailDto>> GetPending(ClientManager manager) => manager.GetPendingRegistrationsAsync();
    private static async Task<ApiResult<ClientRegistrationResultDto>> Approve(Guid id, ClientApprovalDto? dto, ClientManager manager) => await manager.ApproveAsync(id, dto?.SecretExpirationDays ?? 180);
    private static async Task<ApiResult<ClientDetailDto>> Update(Guid id, ClientUpdateDto dto, ClientManager manager) => (await manager.UpdateAsync(id, dto)) is { } x ? x : Problem("Failed to update client", StatusCodes.Status400BadRequest);
    private static async Task<ApiResult<bool>> Delete(Guid id, ClientManager manager) => await manager.DeleteAsync(id) ? TypedResults.NoContent() : Problem("Failed to delete client", StatusCodes.Status400BadRequest);
    private static async Task<ApiResult<ClientSecretDto>> Rotate(Guid id, ClientManager manager) => (await manager.RotateSecretAsync(id)) is { } x ? new ClientSecretDto { Secret = x } : Problem("Failed to rotate secret", StatusCodes.Status400BadRequest);
    private static Task<List<ClientSecretHistoryDto>> GetSecrets(Guid id, ClientManager manager) => manager.GetSecretsAsync(id);
    private static async Task<ApiResult<bool>> AssignScopes(Guid id, ClientScopeAssignDto dto, ClientManager manager) => await manager.AssignScopesAsync(id, dto.ScopeIds) ? TypedResults.NoContent() : Problem("Failed to assign scopes to client", StatusCodes.Status400BadRequest);
    private static Task<List<AuthorizationItemDto>> GetAuthorizations(Guid id, ClientManager manager) => manager.GetAuthorizationsAsync(id);
    private static async Task<ApiResult<bool>> AssignPermissions(Guid id, List<string> codes, PermissionManager manager) => await manager.AssignClientPermissionsAsync(id, codes) ? TypedResults.NoContent() : Problem("Failed to assign client permissions", StatusCodes.Status400BadRequest);
    private static Task<List<string>> GetPermissions(Guid id, PermissionManager manager) => manager.GetClientPermissionCodesAsync(id);
    private static Task<List<PermissionTreeNodeDto>> GetPermissionTree(Guid id, [AsParameters] PermissionFilterDto filter, int? pageIndex, int? pageSize, string? orderBy, PermissionManager manager) => manager.GetClientPermissionTreeAsync(id, FilterQuery.Apply(filter, pageIndex, pageSize, orderBy));
    private static async Task<ApiResult<bool>> SyncMenuPermissions(Guid id, ClientPermissionSyncDto dto, PermissionManager manager, HttpContext context) => await manager.SyncClientMenuPermissionsAsync(id, dto, context.Connection.RemoteIpAddress?.ToString(), context.Request.Headers.UserAgent.ToString()) ? TypedResults.NoContent() : Problem("Failed to synchronize client menu permissions", StatusCodes.Status400BadRequest);
}
