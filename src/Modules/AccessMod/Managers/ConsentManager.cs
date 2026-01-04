using IdentityMod;

namespace AccessMod.Managers;

/// <summary>
/// Manager for user consent operations
/// </summary>
public class ConsentManager(DefaultDbContext dbContext, ILogger<ConsentManager> logger)
    : ManagerBase<DefaultDbContext>(dbContext, logger)
{
    /// <summary>
    /// Check if user has valid consent for the client and scopes
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="clientId">Client ID</param>
    /// <param name="requestedScopes">Requested scopes (space-separated)</param>
    /// <returns>True if user has valid consent</returns>
    public async Task<bool> HasValidConsentAsync(string userId, Guid clientId, string requestedScopes)
    {
        var requestedScopeList = requestedScopes?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
        
        // Find authorizations for this user and client
        var authorizations = await _dbContext.Authorizations
            .Where(a => a.SubjectId == userId 
                && a.ClientId == clientId 
                && a.Status == OAuthConstants.AuthorizationStatuses.Valid
                && (a.Type == OAuthConstants.AuthorizationTypes.Permanent 
                    || a.Type == OAuthConstants.AuthorizationTypes.AdHoc))
            .ToListAsync();

        if (authorizations.Count == 0)
        {
            return false;
        }

        // Check if any authorization covers all requested scopes
        foreach (var auth in authorizations)
        {
            // Check if authorization is expired
            if (auth.ExpirationDate.HasValue && auth.ExpirationDate.Value < DateTimeOffset.UtcNow)
            {
                continue;
            }

            var grantedScopes = auth.Scopes?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
            
            // Check if all requested scopes are in granted scopes
            var allScopesGranted = requestedScopeList.All(rs => grantedScopes.Contains(rs));
            
            if (allScopesGranted)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Create or update user consent
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="clientId">Client ID</param>
    /// <param name="scopes">Granted scopes (space-separated)</param>
    /// <param name="isPermanent">Whether the consent is permanent or temporary</param>
    /// <returns>Created authorization</returns>
    public async Task<Authorization> GrantConsentAsync(string userId, Guid clientId, string scopes, bool isPermanent)
    {
        var authorizationType = isPermanent ? OAuthConstants.AuthorizationTypes.Permanent : OAuthConstants.AuthorizationTypes.AdHoc;
        var expirationDate = isPermanent ? (DateTimeOffset?)null : DateTimeOffset.UtcNow.AddDays(30);

        // Check if an existing authorization already exists for this user/client/scopes combination
        var existingAuth = await _dbContext.Authorizations
            .FirstOrDefaultAsync(a => a.SubjectId == userId 
                && a.ClientId == clientId 
                && a.Scopes == scopes
                && a.Status == OAuthConstants.AuthorizationStatuses.Valid
                && (a.Type == authorizationType || a.Type == OAuthConstants.AuthorizationTypes.Permanent));

        if (existingAuth != null)
        {
            // Update existing authorization
            existingAuth.Type = authorizationType;
            existingAuth.ExpirationDate = expirationDate;
            await _dbContext.SaveChangesAsync();
            return existingAuth;
        }

        // Create new authorization
        var authorization = new Authorization
        {
            SubjectId = userId,
            ClientId = clientId,
            Type = authorizationType,
            Status = OAuthConstants.AuthorizationStatuses.Valid,
            Scopes = scopes,
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = expirationDate
        };

        await _dbContext.Authorizations.AddAsync(authorization);
        await _dbContext.SaveChangesAsync();

        return authorization;
    }

    /// <summary>
    /// Revoke user consent for a specific client
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="clientId">Client ID</param>
    public async Task<bool> RevokeConsentAsync(string userId, Guid clientId)
    {
        var authorizations = await _dbContext.Authorizations
            .Where(a => a.SubjectId == userId 
                && a.ClientId == clientId 
                && a.Status == OAuthConstants.AuthorizationStatuses.Valid)
            .ToListAsync();

        if (authorizations.Count == 0)
        {
            return false;
        }

        foreach (var auth in authorizations)
        {
            auth.Status = OAuthConstants.AuthorizationStatuses.Revoked;
        }

        await _dbContext.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Get user's consent history
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of authorizations</returns>
    public async Task<List<Authorization>> GetUserConsentsAsync(string userId)
    {
        return await _dbContext.Authorizations
            .Include(a => a.Client)
            .Where(a => a.SubjectId == userId 
                && (a.Type == OAuthConstants.AuthorizationTypes.Permanent || a.Type == OAuthConstants.AuthorizationTypes.AdHoc)
                && a.Status == OAuthConstants.AuthorizationStatuses.Valid)
            .OrderByDescending(a => a.CreationDate)
            .ToListAsync();
    }

    /// <summary>
    /// Revoke specific authorization by ID
    /// </summary>
    /// <param name="userId">User ID (for security check)</param>
    /// <param name="authorizationId">Authorization ID</param>
    public async Task<bool> RevokeAuthorizationAsync(string userId, Guid authorizationId)
    {
        var authorization = await _dbContext.Authorizations
            .FirstOrDefaultAsync(a => a.Id == authorizationId && a.SubjectId == userId);

        if (authorization == null)
        {
            return false;
        }

        // Check if already revoked
        if (authorization.Status != OAuthConstants.AuthorizationStatuses.Valid)
        {
            return false;
        }

        authorization.Status = OAuthConstants.AuthorizationStatuses.Revoked;
        await _dbContext.SaveChangesAsync();
        return true;
    }
}
