using Entity.IAMMod;
using EntityFramework.AppDbFactory;
using Perigon.PostgreSQL;
using Share.Constants;

namespace IAMMod.Managers;

/// <summary>Manager for user consent operations.</summary>
public class ConsentManager(
    AppDbFactory dbContextFactory,
    IUserContext userContext,
    ILogger<ConsentManager> logger
) : ManagerBase<DefaultDbContext, Authorization>(dbContextFactory, userContext, logger)
{
    public override Task<bool> HasPermissionAsync(Guid id) => Task.FromResult(_userContext.IsAdmin);

    /// <summary>Check if user has valid consent for the client and scopes.</summary>
    public async Task<bool> HasValidConsentAsync(string userId, Guid clientId, string requestedScopes)
    {
        var requestedScopeList = requestedScopes
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var authorizations = await _dbContext.Authorizations
            .Where(item => item.SubjectId == userId
                && item.ClientId == clientId
                && item.Status == AuthorizationStatuses.Valid
                && (item.Type == AuthorizationTypes.Permanent || item.Type == AuthorizationTypes.AdHoc))
            .ToListAsync();

        foreach (var authorization in authorizations)
        {
            if (authorization.ExpirationDate.HasValue
                && authorization.ExpirationDate.Value < DateTimeOffset.UtcNow)
            {
                continue;
            }

            var grantedScopes = (authorization.Scopes ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (requestedScopeList.All(grantedScopes.Contains))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Create or update user consent.</summary>
    public async Task<Authorization> GrantConsentAsync(string userId, Guid clientId, string scopes, bool isPermanent)
    {
        var authorizationType = isPermanent ? AuthorizationTypes.Permanent : AuthorizationTypes.AdHoc;
        var expirationDate = isPermanent ? (DateTimeOffset?)null : DateTimeOffset.UtcNow.AddDays(30);
        var existingAuth = await _dbContext.Authorizations
            .Where(item => item.SubjectId == userId
                && item.ClientId == clientId
                && item.Scopes == scopes
                && item.Status == AuthorizationStatuses.Valid
                && (item.Type == authorizationType || item.Type == AuthorizationTypes.Permanent))
            .FirstOrDefaultAsync();

        if (existingAuth is not null)
        {
            var updatedAt = DateTimeOffset.UtcNow;
            await _dbContext.Authorizations
                .Where(item => item.Id == existingAuth.Id && item.Status == AuthorizationStatuses.Valid)
                .ExecuteUpdateAsync(update => update
                    .Set(item => item.Type, authorizationType)
                    .Set(item => item.ExpirationDate, expirationDate)
                    .Set(item => item.UpdatedTime, updatedAt));
            existingAuth.Type = authorizationType;
            existingAuth.ExpirationDate = expirationDate;
            existingAuth.UpdatedTime = updatedAt;
            return existingAuth;
        }

        var authorization = new Authorization
        {
            SubjectId = userId,
            ClientId = clientId,
            Type = authorizationType,
            Status = AuthorizationStatuses.Valid,
            Scopes = scopes,
            CreationDate = DateTimeOffset.UtcNow,
            ExpirationDate = expirationDate,
        };
        await _dbContext.TransactionAsync(ct => _dbContext.Authorizations.InsertAsync(authorization, ct));
        return authorization;
    }

    /// <summary>Revoke all valid consent grants for a client.</summary>
    public async Task<bool> RevokeConsentAsync(string userId, Guid clientId)
    {
        var revokedAt = DateTimeOffset.UtcNow;
        var count = await _dbContext.Authorizations
            .Where(item => item.SubjectId == userId
                && item.ClientId == clientId
                && item.Status == AuthorizationStatuses.Valid)
            .ExecuteUpdateAsync(update => update
                .Set(item => item.Status, AuthorizationStatuses.Revoked)
                .Set(item => item.UpdatedTime, revokedAt));
        return count > 0;
    }

    /// <summary>Get the user's active consent history with client details.</summary>
    public async Task<List<Authorization>> GetUserConsentsAsync(string userId)
    {
        var authorizations = await _dbContext.Authorizations
            .Where(item => item.SubjectId == userId
                && (item.Type == AuthorizationTypes.Permanent || item.Type == AuthorizationTypes.AdHoc)
                && item.Status == AuthorizationStatuses.Valid)
            .OrderByDescending(item => item.CreationDate)
            .ToListAsync();
        if (authorizations.Count == 0)
        {
            return authorizations;
        }

        var clientIds = authorizations.Select(item => item.ClientId).Distinct().ToArray();
        var clients = await _dbContext.Clients
            .Where(item => Enumerable.Contains(clientIds, item.Id))
            .ToListAsync();
        var clientsById = clients.ToDictionary(item => item.Id);
        foreach (var authorization in authorizations)
        {
            if (clientsById.TryGetValue(authorization.ClientId, out var client))
            {
                authorization.Client = client;
            }
        }
        return authorizations;
    }

    /// <summary>Revoke one authorization owned by the user.</summary>
    public async Task<bool> RevokeAuthorizationAsync(string userId, Guid authorizationId)
    {
        var revokedAt = DateTimeOffset.UtcNow;
        var count = await _dbContext.Authorizations
            .Where(item => item.Id == authorizationId
                && item.SubjectId == userId
                && item.Status == AuthorizationStatuses.Valid)
            .ExecuteUpdateAsync(update => update
                .Set(item => item.Status, AuthorizationStatuses.Revoked)
                .Set(item => item.UpdatedTime, revokedAt));
        return count > 0;
    }
}
