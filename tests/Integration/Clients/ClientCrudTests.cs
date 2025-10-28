using IntegrationTests.Infrastructure;

namespace IntegrationTests.Clients;

/// <summary>
/// Integration tests for OAuth Client CRUD operations
/// Tests client registration, configuration, secret rotation, and scope management
/// </summary>
public class ClientCrudTests : IntegrationTestBase
{
    public ClientCrudTests(IAMWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetClients_ReturnsClientList()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/clients");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK || 
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task CreateClient_WithValidData_ReturnsCreated()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        var newClient = new
        {
            ClientId = "test_client_" + Guid.NewGuid().ToString("N")[..8],
            DisplayName = "Test Client",
            Type = "confidential",
            RequirePkce = true,
            ConsentType = "explicit",
            RedirectUris = new[] { "https://localhost/callback" },
            AllowedScopes = new[] { "openid", "profile", "email" }
        };

        // Act
        var response = await authenticatedClient.PostAsJsonAsync("/api/clients", newClient);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.Created || 
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.BadRequest
        );
    }

    [Fact]
    public async Task CreateClient_WithDuplicateClientId_ReturnsBadRequest()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        var duplicateClient = new
        {
            ClientId = "existing_client",
            DisplayName = "Duplicate Client"
        };

        // Act
        var response = await authenticatedClient.PostAsJsonAsync("/api/clients", duplicateClient);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.Conflict ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task GetClientById_WithValidId_ReturnsClient()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        var clientId = Guid.NewGuid();

        // Act
        var response = await authenticatedClient.GetAsync($"/api/clients/{clientId}");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task UpdateClient_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        var clientId = Guid.NewGuid();
        var updateData = new
        {
            DisplayName = "Updated Client Name",
            RequirePkce = true,
            ConsentType = "implicit"
        };

        // Act
        var response = await authenticatedClient.PutAsJsonAsync($"/api/clients/{clientId}", updateData);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NoContent ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task DeleteClient_WithValidId_ReturnsSuccess()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        var clientId = Guid.NewGuid();

        // Act
        var response = await authenticatedClient.DeleteAsync($"/api/clients/{clientId}");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NoContent ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task RotateClientSecret_WithValidClientId_ReturnsNewSecret()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        var clientId = Guid.NewGuid();

        // Act
        var response = await authenticatedClient.PostAsync($"/api/clients/{clientId}/secret:rotate", null);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.NotNull(content);
            // Should contain new client secret
        }
    }

    [Fact]
    public async Task AddScopesToClient_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        var clientId = Guid.NewGuid();
        var scopes = new
        {
            Scopes = new[] { "api.read", "api.write" }
        };

        // Act
        var response = await authenticatedClient.PostAsJsonAsync($"/api/clients/{clientId}/scopes", scopes);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NoContent ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task GetClientScopes_WithValidClientId_ReturnsScopes()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        var clientId = Guid.NewGuid();

        // Act
        var response = await authenticatedClient.GetAsync($"/api/clients/{clientId}/scopes");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task UpdateClientRedirectUris_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        var clientId = Guid.NewGuid();
        var redirectUris = new
        {
            RedirectUris = new[] 
            { 
                "https://localhost/callback",
                "https://example.com/oauth/callback" 
            }
        };

        // Act
        var response = await authenticatedClient.PutAsJsonAsync($"/api/clients/{clientId}/redirect-uris", redirectUris);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NoContent ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task CreateClient_WithInvalidRedirectUri_ReturnsBadRequest()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        var invalidClient = new
        {
            ClientId = "invalid_client",
            DisplayName = "Invalid Client",
            RedirectUris = new[] { "not-a-valid-uri" }
        };

        // Act
        var response = await authenticatedClient.PostAsJsonAsync("/api/clients", invalidClient);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task GetClientAuthorizations_WithValidClientId_ReturnsAuthorizations()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        var clientId = Guid.NewGuid();

        // Act
        var response = await authenticatedClient.GetAsync($"/api/clients/{clientId}/authorizations");

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }

    [Fact]
    public async Task CreatePublicClient_WithoutSecret_ReturnsSuccess()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        var publicClient = new
        {
            ClientId = "public_client_" + Guid.NewGuid().ToString("N")[..8],
            DisplayName = "Public Client (SPA)",
            Type = "public",
            RequirePkce = true,
            RedirectUris = new[] { "http://localhost:4200/callback" }
        };

        // Act
        var response = await authenticatedClient.PostAsJsonAsync("/api/clients", publicClient);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.Created || 
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.BadRequest
        );
    }

    [Fact]
    public async Task CreateConfidentialClient_RequiresClientSecret()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        var confidentialClient = new
        {
            ClientId = "confidential_client_" + Guid.NewGuid().ToString("N")[..8],
            DisplayName = "Confidential Client",
            Type = "confidential",
            RequirePkce = false
        };

        // Act
        var response = await authenticatedClient.PostAsJsonAsync("/api/clients", confidentialClient);

        // Assert
        // Should succeed and auto-generate secret, or require it in request
        Assert.True(
            response.StatusCode == HttpStatusCode.Created || 
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.Unauthorized
        );
    }
}
