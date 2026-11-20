namespace EntityFramework.AppDbContext;

/// <summary>
/// 只读数据库上下文
/// </summary>
public abstract class ReadonlyDbContext : ContextBase
{
    public ReadonlyDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // 全局过滤
        base.OnModelCreating(builder);
    }
}
