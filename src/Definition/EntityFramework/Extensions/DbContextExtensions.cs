using EFCore.BulkExtensions;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace EntityFramework.Extensions;

public static class DbContextExtensions
{
    /// <summary>
    /// Partially updates an entity using a DTO, only updating non-null properties
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TUpdateDto">Update DTO type</typeparam>
    /// <param name="context">DbContext instance</param>
    /// <param name="id">Entity ID</param>
    /// <param name="dto">Update DTO with changes</param>
    /// <param name="updateTime">Whether to update UpdatedTime property</param>
    /// <returns>Number of affected rows</returns>
    public static async Task<int> PartialUpdateAsync<TEntity, TUpdateDto>(
        this DbContext context,
        Guid id,
        TUpdateDto dto,
        bool updateTime = true
    )
        where TEntity : class, IEntityBase
        where TUpdateDto : class
    {
        var entity = await context.Set<TEntity>().FindAsync(id);
        if (entity == null)
        {
            return 0;
        }

        // Merge DTO properties into entity
        dto.Adapt(entity);

        if (updateTime)
        {
            entity.UpdatedTime = DateTime.UtcNow;
        }

        return await context.SaveChangesAsync();
    }
}
