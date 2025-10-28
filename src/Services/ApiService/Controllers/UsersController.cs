using IdentityMod.Managers;
using IdentityMod.Models.UserDtos;
using Microsoft.AspNetCore.Authorization;

namespace ApiService.Controllers;

/// <summary>
/// User management controller
/// </summary>
/// <remarks>
/// Provides comprehensive user management operations including:
/// - User CRUD operations
/// - Password management
/// - Role assignment
/// - User status and lockout management
/// 
/// All endpoints require authentication unless specified otherwise.
/// Most operations require appropriate permissions.
/// </remarks>
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class UsersController(
    Share.Localizer localizer,
    UserManager manager,
    IUserContext user,
    ILogger<UsersController> logger
) : RestControllerBase<UserManager>(localizer, manager, user, logger)
{
    /// <summary>
    /// Get paged users
    /// </summary>
    /// <param name="filter">Filter criteria including search, pagination, and sorting options</param>
    /// <returns>Paged list of users</returns>
    /// <response code="200">Returns the paged list of users</response>
    /// <response code="400">If the filter parameters are invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks permission to list users</response>
    /// <example>
    /// GET /api/users?page=1&amp;pageSize=20&amp;search=john
    /// </example>
    [HttpGet]
    [ProducesResponseType(typeof(PageList<UserItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PageList<UserItemDto>>> GetUsers(
        [FromQuery] UserFilterDto filter
    )
    {
        var result = await _manager.GetPageAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Get user detail by id
    /// </summary>
    /// <param name="id">User unique identifier</param>
    /// <returns>User detail information</returns>
    /// <response code="200">Returns the user details</response>
    /// <response code="404">If the user is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks permission to view user details</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserDetailDto>> GetDetail(Guid id)
    {
        var result = await _manager.GetDetailAsync(id);
        return result == null ? NotFound("User not found") : Ok(result);
    }

    /// <summary>
    /// Get user by username
    /// </summary>
    /// <param name="username">Username</param>
    /// <returns>User detail</returns>
    [HttpGet("username/{username}")]
    public async Task<ActionResult<UserDetailDto>> GetByUserName(string username)
    {
        var result = await _manager.GetByUserNameAsync(username);
        return result == null ? NotFound("User not found") : Ok(result);
    }

    /// <summary>
    /// Create new user
    /// </summary>
    /// <param name="dto">User creation data including username, email, and password</param>
    /// <returns>Created user detail</returns>
    /// <response code="201">Returns the newly created user</response>
    /// <response code="400">If the user data is invalid or username/email already exists</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks permission to create users</response>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/users
    ///     {
    ///         "userName": "johndoe",
    ///         "email": "john.doe@example.com",
    ///         "phoneNumber": "1234567890",
    ///         "password": "SecurePassword@123",
    ///         "emailConfirmed": false,
    ///         "lockoutEnabled": true
    ///     }
    /// 
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserDetailDto>> CreateUser([FromBody] UserAddDto dto)
    {
        var result = await _manager.AddAsync(dto);
        return result == null
            ? BadRequest(_manager.ErrorMsg)
            : CreatedAtAction(nameof(GetDetail), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update user
    /// </summary>
    /// <param name="id">User unique identifier</param>
    /// <param name="dto">Update data (only fields to be updated)</param>
    /// <returns>Updated user detail</returns>
    /// <response code="200">Returns the updated user</response>
    /// <response code="400">If the update data is invalid</response>
    /// <response code="404">If the user is not found</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user lacks permission to update users</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserDetailDto>> UpdateUser(
        Guid id,
        [FromBody] UserUpdateDto dto
    )
    {
        var result = await _manager.UpdateAsync(id, dto);
        return result == null ? BadRequest(_manager.ErrorMsg) : Ok(result);
    }

    /// <summary>
    /// Update user status (lock/unlock)
    /// </summary>
    /// <param name="id">User id</param>
    /// <param name="lockoutEnd">Lockout end date (null to unlock)</param>
    /// <returns>No content if successful</returns>
    [HttpPatch("{id}/status")]
    public async Task<ActionResult> UpdateStatus(Guid id, [FromBody] DateTimeOffset? lockoutEnd)
    {
        var success = await _manager.SetLockoutAsync(id, lockoutEnd);
        return !success ? BadRequest(_manager.ErrorMsg) : NoContent();
    }

    /// <summary>
    /// Delete user
    /// </summary>
    /// <param name="id">User id</param>
    /// <param name="hardDelete">Perform hard delete (default false)</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteUser(Guid id, [FromQuery] bool hardDelete = false)
    {
        var success = await _manager.DeleteAsync(id, !hardDelete);
        return !success ? BadRequest(_manager.ErrorMsg) : NoContent();
    }

    /// <summary>
    /// Change user password
    /// </summary>
    /// <param name="id">User id</param>
    /// <param name="newPassword">New password</param>
    /// <returns>No content if successful</returns>
    [HttpPost("{id}/password")]
    public async Task<ActionResult> ChangePassword(Guid id, [FromBody] string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            return BadRequest("Password cannot be empty");
        }

        var success = await _manager.ChangePasswordAsync(id, newPassword);
        return !success ? BadRequest(_manager.ErrorMsg) : NoContent();
    }

    /// <summary>
    /// Assign roles to user
    /// </summary>
    /// <param name="id">User id</param>
    /// <param name="roleIds">Role ids to assign</param>
    /// <returns>No content if successful</returns>
    [HttpPost("{id}/roles")]
    public async Task<ActionResult> AssignRoles(Guid id, [FromBody] List<Guid> roleIds)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        
        var success = await _manager.AssignRolesAsync(id, roleIds, ipAddress, userAgent);
        return !success ? BadRequest(_manager.ErrorMsg) : NoContent();
    }
}
