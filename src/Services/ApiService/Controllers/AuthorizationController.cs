using AccessMod.Managers;
using Entity.AccessMod;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiService.Controllers;

/// <summary>
/// Authorization management controller for users to view and manage their authorizations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuthorizationController(
    ConsentManager consentManager,
    ILogger<AuthorizationController> logger
) : ControllerBase
{
    private readonly ConsentManager _consentManager = consentManager;
    private readonly ILogger<AuthorizationController> _logger = logger;

    /// <summary>
    /// Get current user's authorizations
    /// </summary>
    /// <returns>List of authorizations</returns>
    [HttpGet]
    public async Task<ActionResult<List<UserAuthorizationDto>>> GetUserAuthorizations()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var authorizations = await _consentManager.GetUserConsentsAsync(userId);
        
        var result = authorizations.Select(a => new UserAuthorizationDto
        {
            Id = a.Id,
            ClientId = a.Client?.ClientId ?? string.Empty,
            ClientName = a.Client?.DisplayName ?? a.Client?.ClientId ?? "Unknown",
            Scopes = a.Scopes ?? string.Empty,
            Type = a.Type ?? string.Empty,
            Status = a.Status ?? string.Empty,
            CreationDate = a.CreationDate,
            ExpirationDate = a.ExpirationDate
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Revoke a specific authorization
    /// </summary>
    /// <param name="id">Authorization ID</param>
    /// <returns>Success or error</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> RevokeAuthorization(Guid id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var result = await _consentManager.RevokeAuthorizationAsync(userId, id);
        
        if (!result)
        {
            return NotFound(new { message = "Authorization not found or already revoked" });
        }

        _logger.LogInformation("Authorization {AuthorizationId} revoked by user {UserId}", id, userId);
        return Ok(new { message = "Authorization revoked successfully" });
    }

    /// <summary>
    /// Revoke all authorizations for a specific client
    /// </summary>
    /// <param name="clientId">Client ID</param>
    /// <returns>Success or error</returns>
    [HttpDelete("client/{clientId}")]
    public async Task<IActionResult> RevokeClientAuthorizations(Guid clientId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var result = await _consentManager.RevokeConsentAsync(userId, clientId);
        
        if (!result)
        {
            return NotFound(new { message = "No authorizations found for this client" });
        }

        _logger.LogInformation("All authorizations for client {ClientId} revoked by user {UserId}", clientId, userId);
        return Ok(new { message = "All authorizations for this client revoked successfully" });
    }

    private string? GetUserId()
    {
        return User.FindFirst("sub")?.Value
            ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
            ?? HttpContext.Session.GetString("UserId");
    }
}

/// <summary>
/// User authorization DTO
/// </summary>
public class UserAuthorizationDto
{
    public Guid Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string Scopes { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreationDate { get; set; }
    public DateTimeOffset? ExpirationDate { get; set; }
}
