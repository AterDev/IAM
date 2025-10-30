using System.Text.Json;
using CommonMod.Managers;
using IdentityMod.Models.UserDtos;

namespace IdentityMod.Managers;

/// <summary>
/// Manager for user operations
/// </summary>
public class UserManager(
    DefaultDbContext dbContext,
    ILogger<UserManager> logger,
    IPasswordHasher passwordHasher,
    AuditLogManager auditLogManager
) : ManagerBase<DefaultDbContext, User>(dbContext, logger)
{
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly AuditLogManager _auditLogManager = auditLogManager;

    /// <summary>
    /// Get paged users
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>Paged list of users</returns>
    public async Task<PageList<UserItemDto>> GetPageAsync(UserFilterDto filter)
    {
        Queryable = Queryable
            .WhereNotNull(filter.UserName, q => q.UserName.Contains(filter.UserName!))
            .WhereNotNull(
                filter.Email,
                q => q.Email != null && q.Email.Contains(filter.Email!)
            )
            .WhereNotNull(
                filter.PhoneNumber,
                q => q.PhoneNumber != null && q.PhoneNumber.Contains(filter.PhoneNumber!)
            )
            .WhereNotNull(
                filter.LockoutEnabled,
                q => q.LockoutEnabled == filter.LockoutEnabled
            )
            .WhereNotNull(filter.StartDate, q => q.CreatedTime >= filter.StartDate)
            .WhereNotNull(filter.EndDate, q => q.CreatedTime <= filter.EndDate);

        return await ToPageAsync<UserFilterDto, UserItemDto>(filter);
    }

    /// <summary>
    /// Get user detail by id
    /// </summary>
    /// <param name="id">User id</param>
    /// <returns>User detail or null</returns>
    public async Task<UserDetailDto?> GetDetailAsync(Guid id)
    {
        return await FindAsync<UserDetailDto>(q => q.Id == id);
    }

    /// <summary>
    /// Get user by username
    /// </summary>
    /// <param name="userName">User name</param>
    /// <returns>User detail or null</returns>
    public async Task<UserDetailDto?> GetByUserNameAsync(string userName)
    {
        var normalizedUserName = userName.ToUpperInvariant();
        return await FindAsync<UserDetailDto>(q => q.NormalizedUserName == normalizedUserName);
    }

    /// <summary>
    /// Add new user
    /// </summary>
    /// <param name="dto">User add DTO</param>
    /// <returns>Created user detail or null</returns>
    public async Task<UserDetailDto?> AddAsync(UserAddDto dto)
    {
        var normalizedUserName = dto.UserName.ToUpperInvariant();

        // Check if username already exists
        if (await ExistAsync(q => q.NormalizedUserName == normalizedUserName))
        {
            ErrorMsg = "Username already exists";
            return null;
        }

        // Check if email already exists
        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var normalizedEmail = dto.Email.ToUpperInvariant();
            if (await ExistAsync(q => q.NormalizedEmail == normalizedEmail))
            {
                ErrorMsg = "Email already exists";
                return null;
            }
        }

        var entity = new User
        {
            UserName = dto.UserName,
            NormalizedUserName = normalizedUserName,
            Email = dto.Email,
            NormalizedEmail = dto.Email?.ToUpperInvariant(),
            EmailConfirmed = dto.EmailConfirmed,
            PhoneNumber = dto.PhoneNumber,
            PhoneNumberConfirmed = dto.PhoneNumberConfirmed,
            LockoutEnabled = dto.LockoutEnabled,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
        };

        // Hash password if provided
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            entity.PasswordHash = _passwordHasher.HashPassword(dto.Password);
        }

        var success = await AddAsync(entity);
        return !success ? null : await GetDetailAsync(entity.Id);
    }

    /// <summary>
    /// Update user
    /// </summary>
    /// <param name="id">User id</param>
    /// <param name="dto">User update DTO</param>
    /// <returns>Updated user detail or null</returns>
    public async Task<UserDetailDto?> UpdateAsync(Guid id, UserUpdateDto dto)
    {
        var entity = await FindAsync(id);
        if (entity == null)
        {
            ErrorMsg = "User not found";
            return null;
        }

        // Check if email already exists (if changing)
        if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != entity.Email)
        {
            var normalizedEmail = dto.Email.ToUpperInvariant();
            if (await ExistAsync(q => q.NormalizedEmail == normalizedEmail && q.Id != id))
            {
                ErrorMsg = "Email already exists";
                return null;
            }
            entity.Email = dto.Email;
            entity.NormalizedEmail = normalizedEmail;
        }

        if (dto.PhoneNumber != null)
        {
            entity.PhoneNumber = dto.PhoneNumber;
        }

        if (dto.EmailConfirmed.HasValue)
        {
            entity.EmailConfirmed = dto.EmailConfirmed.Value;
        }

        if (dto.PhoneNumberConfirmed.HasValue)
        {
            entity.PhoneNumberConfirmed = dto.PhoneNumberConfirmed.Value;
        }

        if (dto.IsTwoFactorEnabled.HasValue)
        {
            entity.IsTwoFactorEnabled = dto.IsTwoFactorEnabled.Value;
        }

        if (dto.LockoutEnabled.HasValue)
        {
            entity.LockoutEnabled = dto.LockoutEnabled.Value;
        }

        entity.ConcurrencyStamp = Guid.NewGuid().ToString();

        var success = await UpdateAsync(entity);
        return !success ? null : await GetDetailAsync(id);
    }

    /// <summary>
    /// Delete user (soft delete)
    /// </summary>
    /// <param name="id">User id</param>
    /// <param name="softDelete">Perform soft delete (default true)</param>
    /// <returns>True if successful</returns>
    public async Task<bool> DeleteAsync(Guid id, bool softDelete = true)
    {
        var entity = await FindAsync(id);
        if (entity == null)
        {
            ErrorMsg = "User not found";
            return false;
        }

        return await DeleteAsync(entity, softDelete);
    }

    /// <summary>
    /// Lock or unlock user
    /// </summary>
    /// <param name="id">User id</param>
    /// <param name="lockoutEnd">Lockout end date (null to unlock)</param>
    /// <returns>True if successful</returns>
    public async Task<bool> SetLockoutAsync(Guid id, DateTimeOffset? lockoutEnd)
    {
        var entity = await FindAsync(id);
        if (entity == null)
        {
            ErrorMsg = "User not found";
            return false;
        }

        entity.LockoutEnd = lockoutEnd;
        return await UpdateAsync(entity);
    }

    /// <summary>
    /// Change user password
    /// </summary>
    /// <param name="id">User id</param>
    /// <param name="newPassword">New password</param>
    /// <returns>True if successful</returns>
    public async Task<bool> ChangePasswordAsync(Guid id, string newPassword)
    {
        var entity = await FindAsync(id);
        if (entity == null)
        {
            ErrorMsg = "User not found";
            return false;
        }

        entity.PasswordHash = _passwordHasher.HashPassword(newPassword);
        entity.SecurityStamp = Guid.NewGuid().ToString();
        entity.ConcurrencyStamp = Guid.NewGuid().ToString();

        return await UpdateAsync(entity);
    }

    /// <summary>
    /// Assign roles to user
    /// </summary>
    /// <param name="userId">User id</param>
    /// <param name="roleIds">Role ids to assign</param>
    /// <param name="ipAddress">IP address for audit log</param>
    /// <param name="userAgent">User agent for audit log</param>
    /// <returns>True if successful</returns>
    public async Task<bool> AssignRolesAsync(
        Guid userId,
        List<Guid> roleIds,
        string? ipAddress = null,
        string? userAgent = null
    )
    {
        var user = await FindAsync(userId);
        if (user == null)
        {
            ErrorMsg = "User not found";
            return false;
        }

        // Load current roles
        await LoadManyAsync(user, u => u.UserRoles);

        // Track changes for audit
        var oldRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();

        // Remove old roles not in the new list
        var toRemove = user.UserRoles.Where(ur => !roleIds.Contains(ur.RoleId)).ToList();
        foreach (var userRole in toRemove)
        {
            _dbContext.Set<UserRole>().Remove(userRole);
        }

        // Add new roles
        var existingRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var toAdd = roleIds.Where(rid => !existingRoleIds.Contains(rid)).ToList();
        foreach (var roleId in toAdd)
        {
            user.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        }

        var result = await SaveChangesAsync() > 0;

        if (result)
        {
            // Write audit log for role assignment changes
            var removed = oldRoleIds.Except(roleIds).ToList();
            var added = roleIds.Except(oldRoleIds).ToList();

            if (removed.Any() || added.Any())
            {
                await _auditLogManager.AddAuditLogAsync(
                    category: "Authorization",
                    eventName: "UserRolesChanged",
                    subjectId: userId.ToString(),
                    payload: JsonSerializer.Serialize(new { added, removed }),
                    ipAddress: ipAddress,
                    userAgent: userAgent
                );
            }
        }

        return result;
    }

    /// <summary>
    /// Validate user credentials
    /// </summary>
    /// <param name="userName">User name</param>
    /// <param name="password">Password to verify</param>
    /// <param name="ipAddress">IP address for audit log</param>
    /// <param name="userAgent">User agent for audit log</param>
    /// <returns>User detail if valid, null otherwise</returns>
    public async Task<UserDetailDto?> ValidateCredentialsAsync(
        string userName,
        string password,
        string? ipAddress = null,
        string? userAgent = null
    )
    {
        var normalizedUserName = userName.ToUpperInvariant();
        var user = await _dbSet
            .Where(q => q.NormalizedUserName == normalizedUserName)
            .SingleOrDefaultAsync();

        if (user == null)
        {
            // Write audit log for failed login - user not found
            await _auditLogManager.AddAuditLogAsync(
                category: "Authentication",
                eventName: "LoginFailed",
                subjectId: userName,
                payload: JsonSerializer.Serialize(new { reason = "UserNotFound" }),
                ipAddress: ipAddress,
                userAgent: userAgent
            );
            ErrorMsg = "Invalid username or password";
            return null;
        }

        // Check if user is locked out
        if (user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow)
        {
            await _auditLogManager.AddAuditLogAsync(
                category: "Authentication",
                eventName: "LoginFailed",
                subjectId: user.Id.ToString(),
                payload: JsonSerializer.Serialize(
                    new
                    {
                        reason = "AccountLocked",
                        lockoutEnd = user.LockoutEnd.Value.ToString("O"),
                    }
                ),
                ipAddress: ipAddress,
                userAgent: userAgent
            );
            ErrorMsg = "Account is locked";
            return null;
        }

        // Verify password
        if (
            string.IsNullOrEmpty(user.PasswordHash)
            || !_passwordHasher.VerifyPassword(user.PasswordHash, password)
        )
        {
            // Increment access failed count
            user.AccessFailedCount++;

            // Lock account after too many failed attempts (e.g., 5)
            if (user.LockoutEnabled && user.AccessFailedCount >= 5)
            {
                user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(30);
            }

            await UpdateAsync(user);

            await _auditLogManager.AddAuditLogAsync(
                category: "Authentication",
                eventName: "LoginFailed",
                subjectId: user.Id.ToString(),
                payload: JsonSerializer.Serialize(
                    new { reason = "InvalidPassword", failedCount = user.AccessFailedCount }
                ),
                ipAddress: ipAddress,
                userAgent: userAgent
            );
            ErrorMsg = "Invalid username or password";
            return null;
        }

        // Reset access failed count on successful login
        user.AccessFailedCount = 0;
        await UpdateAsync(user);

        // Write audit log for successful login
        await _auditLogManager.AddAuditLogAsync(
            category: "Authentication",
            eventName: "LoginSuccess",
            subjectId: user.Id.ToString(),
            payload: JsonSerializer.Serialize(new { userName = user.UserName }),
            ipAddress: ipAddress,
            userAgent: userAgent
        );

        return await GetDetailAsync(user.Id);
    }
}
