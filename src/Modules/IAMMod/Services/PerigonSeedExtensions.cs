using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Perigon.PostgreSQL;

namespace IAMMod.Services;

/// <summary>
/// Predicate overloads used by the idempotent startup seed with Perigon queryables.
/// </summary>
internal static class PerigonSeedExtensions
{
    public static Task<bool> AnyAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        where T : class => source.Where(predicate).AnyAsync(cancellationToken);

    public static Task<T?> FirstOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        where T : class, new() => source.Where(predicate).FirstOrDefaultAsync(cancellationToken);

    public static async Task<T> FirstAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        where T : class, new() => await source.Where(predicate).FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Seed record was not found.");

}
