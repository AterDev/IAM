namespace Tests.IAMMod.Managers;

internal static class TestDbContextFactory
{
    public static DefaultDbContext Create(string databaseName)
    {
        var options = new DbContextOptionsBuilder<DefaultDbContext>()
            .UseInMemoryDatabase(databaseName)
            .EnableSensitiveDataLogging()
            .Options;

        return new DefaultDbContext(options);
    }
}
