using Perigon.PostgreSQL;

namespace Tests;

/// <summary>
/// Compatibility helpers for tests while the production data layer uses Perigon.PostgreSQL's
/// explicit write API instead of EF change tracking.
/// </summary>
internal static class TestDbContextExtensions
{
    public static void Add<TEntity>(this DbSet<TEntity> set, TEntity entity)
        where TEntity : class, new() => set.InsertAsync(entity, false, CancellationToken.None).GetAwaiter().GetResult();

    public static void AddRange<TEntity>(this DbSet<TEntity> set, params TEntity[] entities)
        where TEntity : class, new()
    {
        foreach (var entity in entities)
        {
            set.InsertAsync(entity, false, CancellationToken.None).GetAwaiter().GetResult();
        }
    }

    public static Task SaveChangesAsync(this global::Perigon.PostgreSQL.DbContext context) => Task.CompletedTask;

    public static void SaveChanges(this global::Perigon.PostgreSQL.DbContext context)
    {
    }

    public static IQueryable<TEntity> IgnoreQueryFilters<TEntity>(this DbSet<TEntity> set)
        where TEntity : class => set.Context.SetIgnoringAllFilters<TEntity>();

    public static IQueryable<TEntity> AsNoTracking<TEntity>(this DbSet<TEntity> set)
        where TEntity : class => set;

    public static async Task<TEntity> SingleAsync<TEntity>(this IQueryable<TEntity> source, CancellationToken cancellationToken = default)
        where TEntity : class, new()
    {
        var rows = await source.Take(2).ToListAsync(cancellationToken);
        return rows.Count == 1 ? rows[0] : throw new InvalidOperationException("Sequence does not contain exactly one element.");
    }

    public static async Task<TEntity> SingleAsync<TEntity>(this IQueryable<TEntity> source, Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        where TEntity : class, new() => await source.Where(predicate).SingleAsync(cancellationToken);

    public static Task<bool> AnyAsync<TEntity>(this IQueryable<TEntity> source, Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        where TEntity : class => source.Where(predicate).AnyAsync(cancellationToken);
}
