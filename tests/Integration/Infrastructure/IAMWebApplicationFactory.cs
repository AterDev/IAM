using EntityFramework.DBProvider;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace IntegrationTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for integration tests
/// Configures test environment with in-memory database
/// </summary>
public class IAMWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ContextBase>)
            );
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<ContextBase>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase");
            });

            // Build the service provider
            var sp = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database context
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ContextBase>();

            // Ensure the database is created
            db.Database.EnsureCreated();

            // Seed test data if needed
            SeedTestData(db);
        });

        builder.UseEnvironment("Test");
    }

    private static void SeedTestData(ContextBase context)
    {
        // Seed initial test data here if needed
        // For example, test users, roles, clients, etc.
        context.SaveChanges();
    }
}

/// <summary>
/// Base class for integration tests providing common test infrastructure
/// </summary>
public class IntegrationTestBase : IClassFixture<IAMWebApplicationFactory>
{
    protected readonly IAMWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    public IntegrationTestBase(IAMWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    /// <summary>
    /// Helper method to get authenticated client with JWT token
    /// </summary>
    protected async Task<HttpClient> GetAuthenticatedClientAsync(
        string username = "testuser",
        string password = "Test@Password123"
    )
    {
        var client = Factory.CreateClient();

        // First, authenticate to get token
        var loginResponse = await client.PostAsJsonAsync("/connect/token", new
        {
            grant_type = "password",
            username,
            password,
            client_id = "test_client",
            client_secret = "test_secret"
        });

        if (loginResponse.IsSuccessStatusCode)
        {
            var tokenResponse = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
            var accessToken = tokenResponse.GetProperty("access_token").GetString();

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        }

        return client;
    }

    /// <summary>
    /// Helper method to clean up database between tests
    /// </summary>
    protected async Task CleanupDatabaseAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ContextBase>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }
}
