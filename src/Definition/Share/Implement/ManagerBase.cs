using EntityFramework.AppDbContext;
using EntityFramework.AppDbFactory;
using Perigon.PostgreSQL;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Share.Implement;

public abstract class ManagerBase(ILogger logger)
{
    protected readonly ILogger _logger = logger;
}

public abstract class ManagerBase<TDbContext>(TDbContext dbContext, ILogger logger) : ManagerBase(logger)
    where TDbContext : DefaultDbContext
{
    protected readonly TDbContext _dbContext = dbContext;
}

/// <summary>Database operations without change tracking.</summary>
public abstract class ManagerBase<TDbContext, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity> : IDisposable
    where TDbContext : DefaultDbContext
    where TEntity : class, IEntityBase, new()
{
    protected IQueryable<TEntity> Queryable { get; set; }
    private bool _ignoreQueryFilter;
    protected bool IgnoreQueryFilter
    {
        get => _ignoreQueryFilter;
        set
        {
            _ignoreQueryFilter = value;
            Queryable = value ? _dbContext.SetWithoutFilters<TEntity>("SoftDelete") : _dbSet;
        }
    }

    protected readonly ILogger _logger;
    protected TDbContext _dbContext;
    protected DbSet<TEntity> _dbSet;
    protected readonly IUserContext _userContext;

    protected ManagerBase(AppDbFactory dbContextFactory, IUserContext userContext, ILogger logger)
    {
        _logger = logger;
        _userContext = userContext;
        _dbContext = (TDbContext)dbContextFactory.CreateDbContext();
        _dbSet = _dbContext.Set<TEntity>();
        Queryable = _dbSet;
    }

    public Task<TEntity?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbSet.Where(entity => entity.Id == id).FirstOrDefaultAsync(cancellationToken);

    public Task<TEntity?> FindEntityAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
        _dbSet.Where(predicate).FirstOrDefaultAsync(cancellationToken);

    public Task<bool> ExistAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbSet.Where(entity => entity.Id == id).AnyAsync(cancellationToken);

    protected Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        _dbSet.InsertAsync(entity, returning: false, cancellationToken: cancellationToken);

    protected Task BulkInsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) =>
        _dbSet.BulkInsertAsync(entities.ToArray(), cancellationToken: cancellationToken);

    protected Task<int> DeleteOrUpdateAsync(
        IEnumerable<Guid> ids,
        bool softDelete = true,
        CancellationToken cancellationToken = default)
    {
        var keys = ids.Distinct().ToArray();
        if (keys.Length == 0)
            return Task.FromResult(0);
        var query = _dbSet.Where(item => keys.Contains(item.Id));
        return softDelete
            ? query.ExecuteUpdateAsync(update => update.Set(item => item.IsDeleted, true), cancellationToken: cancellationToken)
            : query.ExecuteDeleteAsync(cancellationToken: cancellationToken);
    }

    protected Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default) =>
        _dbContext.TransactionAsync(_ => operation(), cancellationToken);

    protected Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default) =>
        _dbContext.TransactionAsync(_ => operation(), cancellationToken);

    protected void ResetQuery() => Queryable = IgnoreQueryFilter ? _dbContext.SetWithoutFilters<TEntity>("SoftDelete") : _dbSet;

    public abstract Task<bool> HasPermissionAsync(Guid id);

    public void Dispose() => _dbContext.Dispose();
}
