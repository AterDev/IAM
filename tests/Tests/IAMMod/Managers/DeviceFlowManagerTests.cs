namespace Tests.IAMMod.Managers;

public class DeviceFlowManagerTests
{
    [Fact]
    public async Task GetDeviceAuthorizationInteractionAsync_AfterInitiation_ReturnsPendingContext()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(GetDeviceAuthorizationInteractionAsync_AfterInitiation_ReturnsPendingContext));
        var scope = new ApiScope
        {
            Name = "api.read",
            DisplayName = "API Read",
            Description = "Read access",
        };
        var client = new Client
        {
            ClientId = "device-client",
            DisplayName = "Device Client",
            Description = "TV App",
        };

        dbContext.ApiScopes.Add(scope);
        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        var manager = new DeviceFlowManager(dbContext, NullLogger<DeviceFlowManager>.Instance);
        var initiation = await manager.InitiateDeviceAuthorizationAsync(new DeviceAuthorizationRequestDto
        {
            ClientId = client.ClientId,
            Scope = scope.Name,
        });

        Assert.NotNull(initiation);

        var interaction = await manager.GetDeviceAuthorizationInteractionAsync(initiation!.UserCode);

        Assert.Equal("pending", interaction.Status);
        Assert.Equal(client.DisplayName, interaction.ClientName);
        Assert.True(interaction.CanApprove);
        Assert.True(interaction.CanDeny);
        Assert.Contains(interaction.RequestedScopes, s => s.Name == scope.Name);
    }

    [Fact]
    public async Task ApproveDeviceAuthorizationAsync_UpdatesInteractionStatusToApproved()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(ApproveDeviceAuthorizationAsync_UpdatesInteractionStatusToApproved));
        var client = new Client
        {
            ClientId = "device-client-approve",
            DisplayName = "Device Client Approve",
        };

        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        var manager = new DeviceFlowManager(dbContext, NullLogger<DeviceFlowManager>.Instance);
        var initiation = await manager.InitiateDeviceAuthorizationAsync(new DeviceAuthorizationRequestDto
        {
            ClientId = client.ClientId,
            Scope = "openid",
        });

        var approved = await manager.ApproveDeviceAuthorizationAsync(initiation!.UserCode, Guid.NewGuid().ToString());
        var interaction = await manager.GetDeviceAuthorizationInteractionAsync(initiation.UserCode);

        Assert.True(approved);
        Assert.Equal("approved", interaction.Status);
        Assert.False(interaction.CanApprove);
    }

    [Fact]
    public async Task DenyDeviceAuthorizationAsync_UpdatesInteractionStatusToDenied()
    {
        await using var dbContext = TestDbContextFactory.Create(nameof(DenyDeviceAuthorizationAsync_UpdatesInteractionStatusToDenied));
        var client = new Client
        {
            ClientId = "device-client-deny",
            DisplayName = "Device Client Deny",
        };

        dbContext.Clients.Add(client);
        await dbContext.SaveChangesAsync();

        var manager = new DeviceFlowManager(dbContext, NullLogger<DeviceFlowManager>.Instance);
        var initiation = await manager.InitiateDeviceAuthorizationAsync(new DeviceAuthorizationRequestDto
        {
            ClientId = client.ClientId,
            Scope = "openid",
        });

        var denied = await manager.DenyDeviceAuthorizationAsync(initiation!.UserCode);
        var interaction = await manager.GetDeviceAuthorizationInteractionAsync(initiation.UserCode);

        Assert.True(denied);
        Assert.Equal("denied", interaction.Status);
        Assert.False(interaction.CanDeny);
    }
}
