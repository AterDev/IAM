using IAMMod.Models.UserDtos;
using IAMMod.Serialization;
using Microsoft.AspNetCore.Http;
using Share.Exceptions;
using System.Text.RegularExpressions;

namespace IAMMod.Managers;

/// <summary>
/// Manager for user operations
/// </summary>
public class UserManager(
    AppDbFactory dbContextFactory,
    IUserContext userContext,
    ILogger<UserManager> logger,
    AuditLogManager auditLogManager,
    RiskControlService riskControlService,
    SessionValidationService sessionValidationService
) : ManagerBase<DefaultDbContext, User>(dbContextFactory, userContext, logger)
{
    private readonly AuditLogManager _auditLogManager = auditLogManager;
    private readonly RiskControlService _riskControlService = riskControlService;
    private readonly SessionValidationService _sessionValidationService = sessionValidationService;
    private const string SelfServiceProvider = "SelfService";
    private const string PasswordResetTokenName = "PasswordReset";
    private const string ExternalLoginEventCategory = "ExternalAuthentication";

    /// <summary>
    /// Get paged users
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>Paged list of users</returns>
    public async Task<PageList<UserItemDto>> GetPageAsync(UserFilterDto filter)
    {
        var query = _dbSet
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

        var count = await query.CountAsync();
        var users = await query.OrderByDescending(item => item.CreatedTime)
            .Skip((filter.PageIndex - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync();
        return new PageList<UserItemDto>
        {
            Count = count,
            PageIndex = filter.PageIndex,
            Data = users.Select(UserItemDto.FromEntity).ToList(),
        };
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
        var user = await FindAsync(id);
        return user is null ? null : UserDetailDto.FromEntity(user);
    }

    /// <summary>
    /// Get user by username
    /// </summary>
    /// <param name="userName">User name</param>
    /// <returns>User detail or null</returns>
    public async Task<UserDetailDto?> GetByUserNameAsync(string userName)
    {
        var normalizedUserName = userName.Trim().ToUpperInvariant();

        var matchingNames = _dbSet.Where(item => item.NormalizedUserName == normalizedUserName);
        var user = await matchingNames.Where(item => item.UserName == userName)
            .OrderBy(item => item.CreatedTime).ThenBy(item => item.Id).FirstOrDefaultAsync();
        user ??= await matchingNames.OrderBy(item => item.CreatedTime).ThenBy(item => item.Id).FirstOrDefaultAsync();
        return user is null ? null : UserDetailDto.FromEntity(user);
    }

    /// <summary>
    /// Add new user
    /// </summary>
    /// <param name="dto">User add DTO</param>
    /// <returns>Created user detail or null</returns>
    public async Task<UserDetailDto?> AddAsync(UserAddDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            throw new BusinessException(Localizer.BadRequest, StatusCodes.Status400BadRequest);
        }

        var userName = dto.UserName.Trim();
        var normalizedUserName = userName.ToUpperInvariant();
        var email = dto.Email.Trim();
        var normalizedEmail = email.ToUpperInvariant();

        // Check if email already exists
        if (await _dbSet.Where(q => q.NormalizedEmail == normalizedEmail).AnyAsync())
        {
            throw new BusinessException("EmailExists", StatusCodes.Status400BadRequest);
        }

        var entity = new User
        {
            UserName = userName,
            NormalizedUserName = normalizedUserName,
            Email = email,
            NormalizedEmail = normalizedEmail,
            PhoneNumber = dto.PhoneNumber,
            EmailConfirmed = dto.EmailConfirmed,
            PhoneNumberConfirmed = dto.PhoneNumberConfirmed,
            LockoutEnabled = dto.LockoutEnabled,
        };
        entity.UserName = userName;
        entity.NormalizedUserName = normalizedUserName;
        entity.Email = email;
        entity.NormalizedEmail = normalizedEmail;
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
                payload: IamJson.Serialize(new IamAuditPayload
                {
                    UserName = createdUser.UserName,
                    Email = createdUser.Email,
                    PhoneNumber = createdUser.PhoneNumber,
                }),
                ipAddress: ipAddress,
                userAgent: userAgent
            );
        }

        return createdUser;
    }

    /// <summary>
    /// Resolve an external login to a local account, creating one when necessary.
    /// </summary>
    public async Task<ExternalLoginResolutionResultDto> ResolveExternalLoginAsync(
        string provider,
        string providerKey,
        string? email,
        string? displayName,
        string? ipAddress = null,
        string? userAgent = null
    )
    {
        var normalizedProvider = provider.Trim();
        var normalizedProviderKey = providerKey.Trim();
        var normalizedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(normalizedProvider) || string.IsNullOrWhiteSpace(normalizedProviderKey))
        {
            return new ExternalLoginResolutionResultDto
            {
                Status = "invalid_external_identity",
                Provider = provider,
                Message = "Missing provider identity.",
            };
        }

        var existingLogin = await _dbContext.UserLogins
            .Where(q => q.LoginProvider == normalizedProvider && q.ProviderKey == normalizedProviderKey)
            .FirstOrDefaultAsync();
        var existingLoginUser = existingLogin is null
            ? null
            : await _dbContext.Users.Where(q => q.Id == existingLogin.UserId).FirstOrDefaultAsync();

        if (existingLogin is not null && existingLoginUser is not null)
        {
            if (IsUserLocked(existingLoginUser))
            {
                await WriteExternalAuditAsync(
                    "ExternalLoginRejected",
                    existingLoginUser.Id.ToString(),
                    new IamAuditPayload { Provider = normalizedProvider, Reason = "AccountLocked" },
                    ipAddress,
                    userAgent);

                return new ExternalLoginResolutionResultDto
                {
                    Status = "locked",
                    Provider = normalizedProvider,
                    UserId = existingLogin.UserId,
                    UserName = existingLoginUser.UserName,
                    Email = existingLoginUser.Email,
                    Message = "The local account is locked.",
                };
            }

            if (!string.IsNullOrWhiteSpace(email) && !string.Equals(existingLoginUser.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                var updatedEmail = email.Trim();
                var updatedNormalizedEmail = updatedEmail.ToUpperInvariant();

                var emailOwner = await _dbSet
                    .Where(q => q.NormalizedEmail == updatedNormalizedEmail && q.Id != existingLogin.UserId)
                    .FirstOrDefaultAsync();

                if (emailOwner != null)
                {
                    await WriteExternalAuditAsync(
                        "ExternalLoginConflict",
                        existingLogin.UserId.ToString(),
                        new IamAuditPayload
                        {
                            Provider = normalizedProvider,
                            Email = updatedEmail,
                            ConflictingUserId = emailOwner.Id,
                        },
                        ipAddress,
                        userAgent);

                    return new ExternalLoginResolutionResultDto
                    {
                        Status = "email_conflict",
                        Provider = normalizedProvider,
                        UserId = emailOwner.Id,
                        UserName = emailOwner.UserName,
                        Email = emailOwner.Email,
                        Message = "An account with the same email already exists.",
                    };
                }

                var updatedAt = DateTimeOffset.UtcNow;
                await _dbContext.Users
                    .Where(q => q.Id == existingLogin.UserId)
                    .ExecuteUpdateAsync(update => update
                        .Set(q => q.Email, updatedEmail)
                        .Set(q => q.NormalizedEmail, updatedNormalizedEmail)
                        .Set(q => q.EmailConfirmed, true)
                        .Set(q => q.UpdatedTime, updatedAt));
                existingLoginUser.Email = updatedEmail;
                existingLoginUser.NormalizedEmail = updatedNormalizedEmail;
                existingLoginUser.EmailConfirmed = true;
            }

            await WriteExternalAuditAsync(
                "ExternalLoginSucceeded",
                existingLogin.UserId.ToString(),
                new IamAuditPayload { Provider = normalizedProvider, Created = false },
                ipAddress,
                userAgent);

            return new ExternalLoginResolutionResultDto
            {
                Status = "success",
                Provider = normalizedProvider,
                UserId = existingLogin.UserId,
                UserName = existingLoginUser.UserName,
                Email = existingLoginUser.Email,
                IsNewUser = false,
            };
        }

        if (!string.IsNullOrWhiteSpace(normalizedEmail))
        {
            var existingUser = await _dbSet.Where(q => q.NormalizedEmail == normalizedEmail).FirstOrDefaultAsync();
            if (existingUser != null)
            {
                await WriteExternalAuditAsync(
                    "ExternalLoginConflict",
                    existingUser.Id.ToString(),
                    new IamAuditPayload { Provider = normalizedProvider, Email = existingUser.Email },
                    ipAddress,
                    userAgent);

                return new ExternalLoginResolutionResultDto
                {
                    Status = "email_conflict",
                    Provider = normalizedProvider,
                    UserId = existingUser.Id,
                    UserName = existingUser.UserName,
                    Email = existingUser.Email,
                    Message = "An account with the same email already exists.",
                };
            }
        }
        else
        {
            return new ExternalLoginResolutionResultDto
            {
                Status = "invalid_external_identity",
                Provider = normalizedProvider,
                Message = "Missing email address.",
            };
        }

        var userName = await GenerateUniqueExternalUserNameAsync(displayName, email, normalizedProvider, normalizedProviderKey);
        var externalEmail = email!.Trim();
        var user = new User
        {
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = externalEmail,
            NormalizedEmail = normalizedEmail!,
            EmailConfirmed = true,
            PhoneNumberConfirmed = false,
            LockoutEnabled = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
        };

        var generatedPassword = OAuthService.GenerateTokenReference();
        var salt = HashCrypto.BuildSalt();
        user.PasswordSalt = salt;
        user.PasswordHash = HashCrypto.GeneratePwd(generatedPassword, salt);

        var userLogin = new UserLogin
        {
            LoginProvider = normalizedProvider,
            ProviderKey = normalizedProviderKey,
            ProviderDisplayName = normalizedProvider,
            User = user,
        };

        await ExecuteInTransactionAsync(async () =>
        {
            await _dbContext.Users.InsertAsync(user, returning: false);
            userLogin.UserId = user.Id;
            await _dbContext.UserLogins.InsertAsync(userLogin, returning: false);
            return true;
        });

        await WriteExternalAuditAsync(
            "ExternalLoginSucceeded",
            user.Id.ToString(),
            new IamAuditPayload { Provider = normalizedProvider, Created = true },
            ipAddress,
            userAgent);

        return new ExternalLoginResolutionResultDto
        {
            Status = "success",
            Provider = normalizedProvider,
            UserId = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            IsNewUser = true,
        };
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
        var email = entity.Email;
        var normalizedEmail = entity.NormalizedEmail;
        if (dto.Email != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                throw new BusinessException(Localizer.BadRequest, StatusCodes.Status400BadRequest);
            }

            var updatedEmail = dto.Email.Trim();
            var updatedNormalizedEmail = updatedEmail.ToUpperInvariant();
            if (!string.Equals(updatedNormalizedEmail, entity.NormalizedEmail, StringComparison.Ordinal)
                && await _dbSet.Where(q => q.NormalizedEmail == updatedNormalizedEmail && q.Id != id).AnyAsync())
            {
                throw new BusinessException("EmailExists", StatusCodes.Status400BadRequest);
            }

            email = updatedEmail;
            normalizedEmail = updatedNormalizedEmail;
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

        await _dbContext.Users
            .Where(item => item.Id == id)
            .ExecuteUpdateAsync(update => update
                .Set(item => item.Email, email)
                .Set(item => item.NormalizedEmail, normalizedEmail)
                .Set(item => item.PhoneNumber, entity.PhoneNumber)
                .Set(item => item.EmailConfirmed, entity.EmailConfirmed)
                .Set(item => item.PhoneNumberConfirmed, entity.PhoneNumberConfirmed)
                .Set(item => item.IsTwoFactorEnabled, entity.IsTwoFactorEnabled)
                .Set(item => item.LockoutEnabled, entity.LockoutEnabled)
                .Set(item => item.ConcurrencyStamp, entity.ConcurrencyStamp)
                .Set(item => item.UpdatedTime, entity.UpdatedTime));
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
        if (lockoutEnd > DateTimeOffset.UtcNow)
        {
            await _dbContext.Users
                .Where(item => item.Id == id)
                .ExecuteUpdateAsync(update => update
                    .Set(item => item.LockoutEnd, entity.LockoutEnd)
                    .Set(item => item.UpdatedTime, entity.UpdatedTime));
            await InvalidateUserCredentialsAsync(id);
        }
        else
        {
            await _dbContext.Users
                .Where(item => item.Id == id)
                .ExecuteUpdateAsync(update => update
                    .Set(item => item.LockoutEnd, entity.LockoutEnd)
                    .Set(item => item.UpdatedTime, entity.UpdatedTime));
        }
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

        await _dbContext.Users
            .Where(item => item.Id == id)
            .ExecuteUpdateAsync(update => update
                .Set(item => item.PasswordSalt, entity.PasswordSalt)
                .Set(item => item.PasswordHash, entity.PasswordHash)
                .Set(item => item.SecurityStamp, entity.SecurityStamp)
                .Set(item => item.ConcurrencyStamp, entity.ConcurrencyStamp)
                .Set(item => item.UpdatedTime, entity.UpdatedTime));
        await InvalidateUserCredentialsAsync(entity.Id);
        return true;
    }

    public async Task<bool> ChangeOwnPasswordAsync(
        Guid id,
        string currentPassword,
        string newPassword,
        string? ipAddress = null,
        string? userAgent = null)
    {
        var entity = await FindAsync(id);
        if (entity == null)
        {
            throw new BusinessException("UserNotFound", StatusCodes.Status404NotFound);
        }

        if (string.IsNullOrWhiteSpace(entity.PasswordHash)
            || string.IsNullOrWhiteSpace(entity.PasswordSalt)
            || !HashCrypto.Validate(currentPassword, entity.PasswordSalt, entity.PasswordHash))
        {
            await _auditLogManager.AddAuditLogAsync(
                category: "Authentication",
                eventName: "PasswordChangeFailed",
                subjectId: entity.Id.ToString(),
                payload: IamJson.Serialize(new IamAuditPayload { Reason = "InvalidCurrentPassword" }),
                ipAddress: ipAddress,
                userAgent: userAgent
            );
            throw new BusinessException(Localizer.InvalidUserOrPassword, StatusCodes.Status400BadRequest);
        }

        await ChangePasswordAsync(id, newPassword);

        await _auditLogManager.AddAuditLogAsync(
            category: "Authentication",
            eventName: "PasswordChanged",
            subjectId: entity.Id.ToString(),
            payload: IamJson.Serialize(new IamAuditPayload { ChangedAt = DateTimeOffset.UtcNow }),
            ipAddress: ipAddress,
            userAgent: userAgent
        );

        return true;
    }

    public async Task<string?> RequestPasswordResetAsync(
        string email,
        string? ipAddress = null,
        string? userAgent = null
    )
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        var user = await _dbSet.Where(q => q.NormalizedEmail == normalizedEmail).FirstOrDefaultAsync();

        await _auditLogManager.AddAuditLogAsync(
            category: "Authentication",
            eventName: "PasswordResetRequested",
            subjectId: user?.Id.ToString() ?? normalizedEmail,
            payload: IamJson.Serialize(new IamAuditPayload { Email = user?.Email ?? email }),
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

        var entity = await _dbContext.UserTokens
            .Where(q => q.UserId == user.Id
                && q.LoginProvider == SelfServiceProvider
                && q.Name == PasswordResetTokenName)
            .FirstOrDefaultAsync();

        var payload = IamJson.Serialize(
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

            await _dbContext.UserTokens.InsertAsync(entity, returning: false);
        }
        else
        {
            await _dbContext.UserTokens
                .Where(item => item.Id == entity.Id)
                .ExecuteUpdateAsync(update => update
                    .Set(item => item.Value, payload)
                    .Set(item => item.UpdatedTime, DateTimeOffset.UtcNow));
        }
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
        var user = await _dbSet.Where(q => q.NormalizedEmail == normalizedEmail).FirstOrDefaultAsync();
        if (user == null)
        {
            throw new BusinessException(Localizer.UserNotFound, StatusCodes.Status404NotFound);
        }

        var entity = await _dbContext.UserTokens
            .Where(q => q.UserId == user.Id
                && q.LoginProvider == SelfServiceProvider
                && q.Name == PasswordResetTokenName)
            .FirstOrDefaultAsync();

        if (entity?.Value == null)
        {
            throw new BusinessException(Localizer.BadRequest, StatusCodes.Status400BadRequest);
        }

        var payload = IamJson.DeserializePasswordReset(entity.Value);
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
                payload: IamJson.Serialize(new IamAuditPayload { Reason = "InvalidCode" }),
                ipAddress: ipAddress,
                userAgent: userAgent
            );
            throw new BusinessException(Localizer.BadRequest, StatusCodes.Status400BadRequest);
        }

        await ChangePasswordAsync(user.Id, newPassword);
        var consumedPayload = IamJson.Serialize(
            payload with { ConsumedAt = DateTimeOffset.UtcNow });
        await _dbContext.UserTokens
            .Where(item => item.Id == entity.Id)
            .ExecuteUpdateAsync(update => update
                .Set(item => item.Value, consumedPayload)
                .Set(item => item.UpdatedTime, DateTimeOffset.UtcNow));

        await _auditLogManager.AddAuditLogAsync(
            category: "Authentication",
            eventName: "PasswordResetCompleted",
            subjectId: user.Id.ToString(),
            payload: IamJson.Serialize(new IamAuditPayload { Email = user.Email }),
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
            var oldRoleIds = await _dbContext.UserRoles
                .Where(item => item.UserId == userId)
                .Select(item => item.RoleId)
                .ToScalarListAsync();
            var desiredRoleIds = roleIds.Distinct().ToArray();
            var toRemove = oldRoleIds.Except(desiredRoleIds).ToArray();
            var toAdd = desiredRoleIds.Except(oldRoleIds).ToArray();
            if (toRemove.Length > 0)
            {
                await _dbContext.UserRoles
                    .Where(item => item.UserId == userId && Enumerable.Contains(toRemove, item.RoleId))
                    .ExecuteDeleteAsync();
            }
            if (toAdd.Length > 0)
            {
                await _dbContext.UserRoles.BulkInsertAsync(
                    toAdd.Select(roleId => new UserRole { UserId = userId, RoleId = roleId }));
            }

            var result = toRemove.Length > 0 || toAdd.Length > 0;

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
                        payload: IamJson.Serialize(new IamAuditPayload { Added = added, Removed = removed }),
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
    /// <param name="userName">Email address</param>
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
        var loginId = userName.Trim();
        var normalizedLoginId = loginId.ToUpperInvariant();
        var matchedUsers = await _dbSet
            .Where(q => q.NormalizedEmail == normalizedLoginId)
            .OrderBy(q => q.CreatedTime)
            .ThenBy(q => q.Id)
            .Take(2)
            .ToListAsync();

        if (matchedUsers.Count > 1)
        {
            var failureWindow = _riskControlService.RegisterLoginFailure(normalizedLoginId, null, ipAddress);
            await _auditLogManager.AddAuditLogAsync(
                category: "Authentication",
                eventName: "LoginFailed",
                subjectId: loginId,
                payload: IamJson.Serialize(new IamAuditPayload
                {
                    Reason = "DuplicateEmail",
                    IpFailureCount = failureWindow.IpFailureCount,
                }),
                ipAddress: ipAddress,
                userAgent: userAgent
            );
            throw new BusinessException(Localizer.InvalidEmailOrPassword, StatusCodes.Status401Unauthorized);
        }

        var user = matchedUsers.SingleOrDefault();

        if (user == null)
        {
            var failureWindow = _riskControlService.RegisterLoginFailure(normalizedLoginId, null, ipAddress);
            // Write audit log for failed login - user not found
            await _auditLogManager.AddAuditLogAsync(
                category: "Authentication",
                eventName: "LoginFailed",
                subjectId: loginId,
                payload: IamJson.Serialize(new IamAuditPayload
                {
                    Reason = "UserNotFound",
                    IpFailureCount = failureWindow.IpFailureCount,
                }),
                ipAddress: ipAddress,
                userAgent: userAgent
            );
            throw new BusinessException(Localizer.InvalidEmailOrPassword, StatusCodes.Status401Unauthorized);
        }

        // Check if user is locked out
        if (user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow)
        {
            await _auditLogManager.AddAuditLogAsync(
                category: "Authentication",
                eventName: "LoginFailed",
                subjectId: user.Id.ToString(),
                payload: IamJson.Serialize(new IamAuditPayload
                {
                    Reason = "AccountLocked",
                    LockoutEnd = user.LockoutEnd.Value.ToString("O"),
                }),
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
            var loginRisk = await _riskControlService.EvaluateLoginRiskAsync(user.Id, ipAddress, userAgent);
            var failureWindow = _riskControlService.RegisterLoginFailure(normalizedLoginId, user.Id, ipAddress);

            // Increment access failed count
            user.AccessFailedCount++;

            // Lock account after too many failed attempts
            if (user.LockoutEnabled && user.AccessFailedCount >= _riskControlService.LoginFailureThreshold)
            {
                user.LockoutEnd = DateTimeOffset.UtcNow.Add(_riskControlService.AccountLockoutDuration);
            }

            user.UpdatedTime = DateTime.UtcNow;
            await _dbContext.Users
                .Where(item => item.Id == user.Id)
                .ExecuteUpdateAsync(update => update
                    .Set(item => item.AccessFailedCount, user.AccessFailedCount)
                    .Set(item => item.LockoutEnd, user.LockoutEnd)
                    .Set(item => item.UpdatedTime, user.UpdatedTime));

            await _auditLogManager.AddAuditLogAsync(
                category: "Authentication",
                eventName: "LoginFailed",
                subjectId: user.Id.ToString(),
                payload: IamJson.Serialize(new IamAuditPayload
                {
                    Reason = "InvalidPassword",
                    FailedCount = user.AccessFailedCount,
                    IpFailureCount = failureWindow.IpFailureCount,
                    HasKnownIp = loginRisk.HasKnownIp,
                    HasKnownUserAgent = loginRisk.HasKnownUserAgent,
                    RequiresStepUp = loginRisk.RequiresStepUp,
                    LastKnownIpAddress = loginRisk.LastKnownIpAddress,
                    LastKnownUserAgent = loginRisk.LastKnownUserAgent,
                }),
                ipAddress: ipAddress,
                userAgent: userAgent
            );
            throw new BusinessException(Localizer.InvalidEmailOrPassword, StatusCodes.Status401Unauthorized);
        }

        // Reset access failed count on successful login
    var successRisk = await _riskControlService.EvaluateLoginRiskAsync(user.Id, ipAddress, userAgent);
    _riskControlService.ResetLoginFailures(normalizedLoginId, user.Id, ipAddress);
        user.AccessFailedCount = 0;
        user.UpdatedTime = DateTime.UtcNow;
        await _dbContext.Users
            .Where(item => item.Id == user.Id)
            .ExecuteUpdateAsync(update => update
                .Set(item => item.AccessFailedCount, 0)
                .Set(item => item.UpdatedTime, user.UpdatedTime));

        // Write audit log for successful login
        await _auditLogManager.AddAuditLogAsync(
            category: "Authentication",
            eventName: "LoginSuccess",
            subjectId: user.Id.ToString(),
            payload: IamJson.Serialize(new IamAuditPayload
            {
                UserName = user.UserName,
                HasKnownIp = successRisk.HasKnownIp,
                HasKnownUserAgent = successRisk.HasKnownUserAgent,
                RequiresStepUp = successRisk.RequiresStepUp,
                LastKnownIpAddress = successRisk.LastKnownIpAddress,
                LastKnownUserAgent = successRisk.LastKnownUserAgent,
            }),
            ipAddress: ipAddress,
            userAgent: userAgent
        );

        if (successRisk.RequiresStepUp)
        {
            await _auditLogManager.AddAuditLogAsync(
                category: "Authentication",
                eventName: "LoginStepUpRecommended",
                subjectId: user.Id.ToString(),
                payload: IamJson.Serialize(new IamAuditPayload
                {
                    UserName = user.UserName,
                    HasKnownIp = successRisk.HasKnownIp,
                    HasKnownUserAgent = successRisk.HasKnownUserAgent,
                    LastKnownIpAddress = successRisk.LastKnownIpAddress,
                    LastKnownUserAgent = successRisk.LastKnownUserAgent,
                }),
                ipAddress: ipAddress,
                userAgent: userAgent
            );
        }

        return await GetDetailAsync(user.Id);
    }

    private static bool IsUserLocked(User user)
    {
        return user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;
    }

    private async Task<string> GenerateUniqueExternalUserNameAsync(
        string? displayName,
        string? email,
        string provider,
        string providerKey)
    {
        var baseName = BuildExternalUserNameBase(displayName, email, provider, providerKey);
        var candidate = baseName;
        var suffix = 1;

        while (await _dbSet.Where(q => q.NormalizedUserName == candidate.ToUpperInvariant()).AnyAsync())
        {
            candidate = $"{baseName}{suffix++}";
        }

        return candidate;
    }

    private static string BuildExternalUserNameBase(
        string? displayName,
        string? email,
        string provider,
        string providerKey)
    {
        var raw = !string.IsNullOrWhiteSpace(email)
            ? email.Split('@', StringSplitOptions.RemoveEmptyEntries)[0]
            : displayName;

        var normalized = Regex.Replace(raw ?? string.Empty, "[^A-Za-z0-9._-]", string.Empty);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            var providerPrefix = Regex.Replace(provider, "[^A-Za-z0-9]", string.Empty);
            var keySuffix = Regex.Replace(providerKey, "[^A-Za-z0-9]", string.Empty);
            keySuffix = keySuffix.Length > 8 ? keySuffix[..8] : keySuffix;
            normalized = $"{providerPrefix}{keySuffix}";
        }

        if (normalized.Length > 32)
        {
            normalized = normalized[..32];
        }

        return normalized;
    }

    private Task AddAuditLogAsync(
        string eventName,
        string subjectId,
        IamAuditPayload payload,
        string? ipAddress,
        string? userAgent)
    {
        return _auditLogManager.AddAuditLogAsync(
            category: ExternalLoginEventCategory,
            eventName: eventName,
            subjectId: subjectId,
            payload: IamJson.Serialize(payload),
            ipAddress: ipAddress,
            userAgent: userAgent);
    }

    private Task WriteExternalAuditAsync(
        string eventName,
        string subjectId,
        IamAuditPayload payload,
        string? ipAddress,
        string? userAgent)
    {
        return AddAuditLogAsync(eventName, subjectId, payload, ipAddress, userAgent);
    }

    private async Task InvalidateUserCredentialsAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        var subjectId = userId.ToString();

        var sessions = await _dbContext.LoginSessions
            .Where(q => q.UserId == userId && q.IsActive)
            .ToListAsync();

        // Revoke credentials even for device flows and legacy tokens without a session.
        await _dbContext.Tokens
            .Where(q => q.SubjectId == subjectId
                && (q.Status == TokenStatuses.Valid || q.Status == TokenStatuses.Pending))
            .ExecuteUpdateAsync(update => update
                .Set(item => item.Status, TokenStatuses.Revoked)
                .Set(item => item.UpdatedTime, now));

        await _dbContext.LoginSessions
            .Where(q => q.UserId == userId && q.IsActive)
            .ExecuteUpdateAsync(update => update
                .Set(item => item.IsActive, false)
                .Set(item => item.UpdatedTime, now));

        foreach (var session in sessions)
        {
            session.IsActive = false;
            session.UpdatedTime = now;
            await _sessionValidationService.SetStateAsync(session);
        }
    }
}
