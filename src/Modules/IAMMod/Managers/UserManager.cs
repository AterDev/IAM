using IAMMod.Models.UserDtos;
using Microsoft.AspNetCore.Http;
using Share.Exceptions;
using System.Text.Json;

namespace IAMMod.Managers;

/// <summary>
/// Manager for user operations
/// </summary>
public class UserManager(
    TenantDbFactory dbContextFactory,
    IUserContext userContext,
    ILogger<UserManager> logger,
    AuditLogManager auditLogManager
) : ManagerBase<DefaultDbContext, User>(dbContextFactory, userContext, logger)
{
    private readonly AuditLogManager _auditLogManager = auditLogManager;
    private const string SelfServiceProvider = "SelfService";
    private const string PasswordResetTokenName = "PasswordReset";

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

        return await PageListAsync<UserFilterDto, UserItemDto>(filter);
    }

    /// <summary>
    /// Check if user has permission to access user
    /// </summary>
    /// <param name="id">User id</param>
    /// <returns>True if has permission</returns>
    public override async Task<bool> HasPermissionAsync(Guid id)
    {
        return await Task.FromResult(_userContext.IsAdmin);
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
        if (await _dbSet.AnyAsync(q => q.NormalizedUserName == normalizedUserName))
        {
            throw new BusinessException("UsernameExists", StatusCodes.Status400BadRequest);
        }

        // Check if email already exists
        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var normalizedEmail = dto.Email.ToUpperInvariant();
            if (await _dbSet.AnyAsync(q => q.NormalizedEmail == normalizedEmail))
            {
                throw new BusinessException("EmailExists", StatusCodes.Status400BadRequest);
            }
        }

        var entity = dto.MapTo<User>();
        entity.NormalizedUserName = normalizedUserName;
        entity.NormalizedEmail = dto.Email?.ToUpperInvariant();
        entity.SecurityStamp = Guid.NewGuid().ToString();
        entity.ConcurrencyStamp = Guid.NewGuid().ToString();

        // Hash password if provided
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            var salt = HashCrypto.BuildSalt();
            entity.PasswordSalt = salt;
            entity.PasswordHash = HashCrypto.GeneratePwd(dto.Password, salt);
        }

        await InsertAsync(entity);
        return await GetDetailAsync(entity.Id);
    }

    public async Task<UserDetailDto?> RegisterSelfServiceAsync(
        UserAddDto dto,
        string? ipAddress = null,
        string? userAgent = null
    )
    {
        dto.EmailConfirmed = false;
        dto.PhoneNumberConfirmed = false;
        dto.LockoutEnabled = true;

        var createdUser = await AddAsync(dto);
        if (createdUser != null)
        {
            await _auditLogManager.AddAuditLogAsync(
                category: "Authentication",
                eventName: "SelfServiceRegister",
                subjectId: createdUser.Id.ToString(),
                payload: JsonSerializer.Serialize(
                    new { createdUser.UserName, createdUser.Email, createdUser.PhoneNumber }
                ),
                ipAddress: ipAddress,
                userAgent: userAgent
            );
        }

        return createdUser;
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
            throw new BusinessException("UserNotFound", StatusCodes.Status404NotFound);
        }

        // Check if email already exists (if changing)
        if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != entity.Email)
        {
            var normalizedEmail = dto.Email.ToUpperInvariant();
            if (await _dbSet.AnyAsync(q => q.NormalizedEmail == normalizedEmail && q.Id != id))
            {
                throw new BusinessException("EmailExists", StatusCodes.Status400BadRequest);
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
        entity.UpdatedTime = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return await GetDetailAsync(id);
    }

    /// <summary>
    /// Delete user (soft delete)
    /// </summary>
    /// <param name="id">User id</param>
    /// <param name="softDelete">Perform soft delete (default true)</param>
    /// <returns>True if successful</returns>
    public async Task<bool> DeleteAsync(Guid id, bool softDelete = true)
    {
        var deleted = await DeleteOrUpdateAsync([id], softDelete);
        if (deleted == 0)
        {
            throw new BusinessException("UserNotFound", StatusCodes.Status404NotFound);
        }
        return true;
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
            throw new BusinessException("UserNotFound", StatusCodes.Status404NotFound);
        }

        entity.LockoutEnd = lockoutEnd;
        entity.UpdatedTime = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return true;
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
            throw new BusinessException("UserNotFound", StatusCodes.Status404NotFound);
        }

        var salt = HashCrypto.BuildSalt();
        entity.PasswordSalt = salt;
        entity.PasswordHash = HashCrypto.GeneratePwd(newPassword, salt);
        entity.SecurityStamp = Guid.NewGuid().ToString();
        entity.ConcurrencyStamp = Guid.NewGuid().ToString();
        entity.UpdatedTime = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<string?> RequestPasswordResetAsync(
        string email,
        string? ipAddress = null,
        string? userAgent = null
    )
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        var user = await _dbSet.FirstOrDefaultAsync(q => q.NormalizedEmail == normalizedEmail);

        await _auditLogManager.AddAuditLogAsync(
            category: "Authentication",
            eventName: "PasswordResetRequested",
            subjectId: user?.Id.ToString() ?? normalizedEmail,
            payload: JsonSerializer.Serialize(new { email = user?.Email ?? email }),
            ipAddress: ipAddress,
            userAgent: userAgent
        );

        if (user == null)
        {
            return null;
        }

        var token = OAuthService.GenerateTokenReference()[..8].ToUpperInvariant();
        var salt = HashCrypto.BuildSalt();
        var hash = HashCrypto.GeneratePwd(token, salt);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);

        var entity = await _dbContext.UserTokens.FirstOrDefaultAsync(q =>
            q.UserId == user.Id
            && q.LoginProvider == SelfServiceProvider
            && q.Name == PasswordResetTokenName
        );

        var payload = JsonSerializer.Serialize(
            new PasswordResetTokenPayload(hash, salt, expiresAt, null, user.SecurityStamp)
        );

        if (entity == null)
        {
            entity = new UserToken
            {
                UserId = user.Id,
                LoginProvider = SelfServiceProvider,
                Name = PasswordResetTokenName,
                Value = payload,
            };

            await _dbContext.UserTokens.AddAsync(entity);
        }
        else
        {
            entity.Value = payload;
            entity.UpdatedTime = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation(
            "Generated password reset code for {Email}. Development code: {Token}",
            user.Email,
            token
        );

        return token;
    }

    public async Task<bool> ResetPasswordAsync(
        string email,
        string code,
        string newPassword,
        string? ipAddress = null,
        string? userAgent = null
    )
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        var user = await _dbSet.FirstOrDefaultAsync(q => q.NormalizedEmail == normalizedEmail);
        if (user == null)
        {
            throw new BusinessException(Localizer.UserNotFound, StatusCodes.Status404NotFound);
        }

        var entity = await _dbContext.UserTokens.FirstOrDefaultAsync(q =>
            q.UserId == user.Id
            && q.LoginProvider == SelfServiceProvider
            && q.Name == PasswordResetTokenName
        );

        if (entity?.Value == null)
        {
            throw new BusinessException(Localizer.BadRequest, StatusCodes.Status400BadRequest);
        }

        var payload = JsonSerializer.Deserialize<PasswordResetTokenPayload>(entity.Value);
        if (payload == null)
        {
            throw new BusinessException(Localizer.BadRequest, StatusCodes.Status400BadRequest);
        }

        if (payload.ConsumedAt.HasValue || payload.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new BusinessException(Localizer.BadRequest, StatusCodes.Status400BadRequest);
        }

        if (!string.Equals(payload.SecurityStamp, user.SecurityStamp, StringComparison.Ordinal))
        {
            throw new BusinessException(Localizer.BadRequest, StatusCodes.Status400BadRequest);
        }

        var validCode = HashCrypto.Validate(code.ToUpperInvariant(), payload.Salt, payload.Hash);
        if (!validCode)
        {
            await _auditLogManager.AddAuditLogAsync(
                category: "Authentication",
                eventName: "PasswordResetFailed",
                subjectId: user.Id.ToString(),
                payload: JsonSerializer.Serialize(new { reason = "InvalidCode" }),
                ipAddress: ipAddress,
                userAgent: userAgent
            );
            throw new BusinessException(Localizer.BadRequest, StatusCodes.Status400BadRequest);
        }

        await ChangePasswordAsync(user.Id, newPassword);
        entity.Value = JsonSerializer.Serialize(payload with { ConsumedAt = DateTimeOffset.UtcNow });
        entity.UpdatedTime = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        await _auditLogManager.AddAuditLogAsync(
            category: "Authentication",
            eventName: "PasswordResetCompleted",
            subjectId: user.Id.ToString(),
            payload: JsonSerializer.Serialize(new { email = user.Email }),
            ipAddress: ipAddress,
            userAgent: userAgent
        );

        return true;
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
            throw new BusinessException("UserNotFound", StatusCodes.Status404NotFound);
        }

        return await ExecuteInTransactionAsync(async () =>
        {
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

            var result = await _dbContext.SaveChangesAsync() > 0;

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
        });
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
            throw new BusinessException(Localizer.UserNotFound, StatusCodes.Status401Unauthorized);
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
            throw new BusinessException(Localizer.LockAccountForManyTimes, StatusCodes.Status403Forbidden);
        }

        // Verify password
        if (
            string.IsNullOrEmpty(user.PasswordHash)
            || !HashCrypto.Validate(password, user.PasswordSalt, user.PasswordHash)
        )
        {
            // Increment access failed count
            user.AccessFailedCount++;

            // Lock account after too many failed attempts (e.g., 5)
            if (user.LockoutEnabled && user.AccessFailedCount >= 5)
            {
                user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(30);
            }

            user.UpdatedTime = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

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
            throw new BusinessException(Localizer.InvalidUserOrPassword, StatusCodes.Status401Unauthorized);
        }

        // Reset access failed count on successful login
        user.AccessFailedCount = 0;
        user.UpdatedTime = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

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

    private sealed record PasswordResetTokenPayload(
        string Hash,
        string Salt,
        DateTimeOffset ExpiresAt,
        DateTimeOffset? ConsumedAt,
        string? SecurityStamp
    );
}
