namespace EntityFramework.AppDbContext;

public abstract class ContextBase(DbContextOptions options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        EnableSoftDeleteFilter<EntityBase>("SoftDelete", entity => !entity.IsDeleted);
    }
}
