using IAMMod.Managers;
using IAMMod.Models.AuthorizationDtos;
using IAMMod.Models.ClientDtos;
using IAMMod.Models.PermissionDtos;
using Microsoft.AspNetCore.Authorization;
using Share.Constants;
namespace ApiService.Controllers.IAMMod;

/// <summary>
/// OAuth/OIDC client management controller
/// </summary>
/// <remarks>
/// Manages OAuth 2.0 and OpenID Connect client applications.
/// 
/// Client types supported:
/// - Confidential: Server-side applications with secure secret storage
/// - Public: Single-page applications (SPAs) and mobile apps without secrets
/// 
/// Features:
/// - Client registration and configuration
/// - Secret rotation for security
/// - Scope assignment for access control
/// - Authorization tracking
/// - PKCE configuration for public clients
/// 
/// All endpoints require appropriate administrative permissions.
/// </remarks>
public class ClientsController(
    Localizer localizer,
    ClientManager manager,
    PermissionManager permissionManager,
    IUserContext user,
    ILogger<ClientsController> logger
) : RestControllerBase<ClientManager>(localizer, manager, user, logger)
{
    private readonly PermissionManager _permissionManager = permissionManager;

    /// <summary>
    /// Get paged clients
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>Paged list of clients</returns>
    [HttpGet]
    public Task<PageList<ClientItemDto>> GetClients(
        [FromQuery] ClientFilterDto filter
    )
    {
        return _manager.GetPageAsync(filter);
    }

    /// <summary>
    /// Get client detail by id
    /// </summary>
    /// <param name="id">Client id</param>
    /// <returns>Client detail</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ClientDetailDto>> GetDetail(Guid id)
    {
        var result = await _manager.GetDetailAsync(id);
        return result == null ? NotFound("Client not found") : Ok(result);
    }

    /// <summary>
    /// Create new client
    /// </summary>
    /// <param name="dto">Client data</param>
    /// <returns>Created client detail with secret</returns>
    [HttpPost]
    [Authorize(Policy = WebConst.AdminUser)]
    public async Task<string?> CreateClient([FromBody] ClientAddDto dto)
    {
        var secret = await _manager.AddAsync(dto);
        return secret;
    }

    /// <summary>
    /// Register a new client for developer self-service review.
    /// </summary>
    [HttpPost("register")]
    [Authorize]
    public async Task<ActionResult<ClientRegistrationResultDto>> RegisterClient([FromBody] ClientRegistrationRequestDto dto)
    {
        var result = await _manager.RegisterAsync(dto);
        return Ok(result);
    }

    /// <summary>
    /// Get clients visible to the current developer portal user.
    /// </summary>
    [HttpGet("my-clients")]
    [Authorize]
    public Task<List<ClientDetailDto>> GetMyClients()
    {
        return _manager.GetMyClientsAsync();
    }

    /// <summary>
    /// Get pending client registration requests.
    /// </summary>
    [HttpGet("pending-registrations")]
    [Authorize(Policy = WebConst.AdminUser)]
    public Task<List<ClientDetailDto>> GetPendingRegistrations()
    {
        return _manager.GetPendingRegistrationsAsync();
    }

    /// <summary>
    /// Approve a pending client registration.
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(Policy = WebConst.AdminUser)]
    public async Task<ActionResult<ClientRegistrationResultDto>> ApproveClient(Guid id, [FromBody] ClientApprovalDto? dto)
    {
        var result = await _manager.ApproveAsync(id, dto?.SecretExpirationDays ?? 180);
        return Ok(result);
    }

    /// <summary>
    /// Update client
    /// </summary>
    /// <param name="id">Client id</param>
    /// <param name="dto">Update data</param>
    /// <returns>Updated client detail</returns>
    [HttpPut("{id}")]
    [Authorize(Policy = WebConst.AdminUser)]
    public async Task<ActionResult<ClientDetailDto>> UpdateClient(
        Guid id,
        [FromBody] ClientUpdateDto dto
    )
    {
        var result = await _manager.UpdateAsync(id, dto);
        return result == null
            ? Problem("Failed to update client", statusCode: StatusCodes.Status400BadRequest)
            : Ok(result);
    }

    /// <summary>
    /// Delete client
    /// </summary>
    /// <param name="id">Client id</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id}")]
    [Authorize(Policy = WebConst.AdminUser)]
    public async Task<ActionResult> DeleteClient(Guid id)
    {
        var success = await _manager.DeleteAsync(id);
        return !success
            ? Problem("Failed to delete client", statusCode: StatusCodes.Status400BadRequest)
            : NoContent();
    }

    /// <summary>
    /// Rotate client secret
    /// </summary>
    /// <param name="id">Client unique identifier</param>
    /// <returns>New client secret (store securely, won't be shown again)</returns>
    /// <response code="200">Returns the new client secret</response>
    /// <response code="400">If the secret rotation fails</response>
    /// <response code="404">If the client is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks permission to rotate secrets</response>
    /// <remarks>
    /// IMPORTANT: The new secret is only shown once. Store it securely immediately.
    /// The old secret will be invalidated and cannot be recovered.
    /// 
    /// This operation should be performed:
    /// - Regularly as a security best practice
    /// - When a secret may have been compromised
    /// - When rotating credentials for compliance
    /// </remarks>
    [HttpPost("{id}/secret:rotate")]
    [Authorize]
    public async Task<ActionResult<ClientSecretDto>> RotateSecret(Guid id)
    {
        var newSecret = await _manager.RotateSecretAsync(id);
        if (newSecret == null)
        {
            return Problem("Failed to rotate secret", statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(new ClientSecretDto { Secret = newSecret });
    }

    /// <summary>
    /// Get client secret history metadata.
    /// </summary>
    [HttpGet("{id}/secrets")]
    [Authorize]
    public Task<List<ClientSecretHistoryDto>> GetSecrets(Guid id)
    {
        return _manager.GetSecretsAsync(id);
    }

    /// <summary>
    /// Assign scopes to client
    /// </summary>
    /// <param name="id">Client id</param>
    /// <param name="dto">Scope assignment data</param>
    /// <returns>No content if successful</returns>
    [HttpPost("{id}/scopes")]
    [Authorize(Policy = WebConst.AdminUser)]
    public async Task<ActionResult> AssignScopes(Guid id, [FromBody] ClientScopeAssignDto dto)
    {
        var success = await _manager.AssignScopesAsync(id, dto.ScopeIds);
        return !success
            ? Problem("Failed to assign scopes to client", statusCode: StatusCodes.Status400BadRequest)
            : NoContent();
    }

    /// <summary>
    /// Get client authorizations
    /// </summary>
    /// <param name="id">Client id</param>
    /// <returns>List of authorizations</returns>
    [HttpGet("{id}/authorizations")]
    [Authorize]
    public Task<List<AuthorizationItemDto>> GetAuthorizations(Guid id)
    {
        return _manager.GetAuthorizationsAsync(id);
    }

    /// <summary>
    /// Replace client permission relations.
    /// </summary>
    [HttpPost("{id}/permissions")]
    [Authorize(Policy = WebConst.AdminUser)]
    public async Task<ActionResult> AssignPermissions(Guid id, [FromBody] List<string> permissionCodes)
    {
        var success = await _permissionManager.AssignClientPermissionsAsync(id, permissionCodes);
        return success ? NoContent() : Problem("Failed to assign client permissions", statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Get client permission codes.
    /// </summary>
    [HttpGet("{id}/permissions")]
    [Authorize(Policy = WebConst.AdminUser)]
    public Task<List<string>> GetPermissions(Guid id)
    {
        return _permissionManager.GetClientPermissionCodesAsync(id);
    }

    /// <summary>
    /// Get client permission tree.
    /// </summary>
    [HttpGet("{id}/permission-tree")]
    [Authorize(Policy = WebConst.AdminUser)]
    public Task<List<PermissionTreeNodeDto>> GetPermissionTree(Guid id, [FromQuery] PermissionFilterDto filter)
    {
        return _permissionManager.GetClientPermissionTreeAsync(id, filter);
    }

    /// <summary>
    /// Full replacement synchronization for client menu/button permissions.
    /// </summary>
    [HttpPost("{id}/menu-permissions:sync")]
    [Authorize(Policy = WebConst.AdminUser)]
    public async Task<ActionResult> SyncMenuPermissions(Guid id, [FromBody] ClientPermissionSyncDto dto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        var success = await _permissionManager.SyncClientMenuPermissionsAsync(id, dto, ipAddress, userAgent);
        return success ? NoContent() : Problem("Failed to synchronize client menu permissions", statusCode: StatusCodes.Status400BadRequest);
    }
}
