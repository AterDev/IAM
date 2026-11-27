using AppHost;
using Microsoft.Extensions.Configuration;
using Perigon.AspNetCore.Constants;

var builder = DistributedApplication.CreateBuilder(args);
var aspireSetting = AppSettingsHelper.LoadAspireSettings(builder.Configuration);
var isTesting = builder.Configuration["ASPIRE_ENVIRONMENT"]?.ToLowerInvariant() == "testing";
var publicOrigin = builder.AddParameter("public-origin");

builder.AddDockerComposeEnvironment("compose")
    .WithDashboard(enabled: false);

IResourceBuilder<IResourceWithConnectionString>? database = null;
IResourceBuilder<IResourceWithConnectionString>? cache = null;

var externalDatabase = builder.Configuration.GetConnectionString(AppConst.Default);
var externalCache = builder.Configuration.GetConnectionString(AppConst.Cache);

if (!string.IsNullOrWhiteSpace(externalDatabase))
{
    database = builder.AddConnectionString(AppConst.Default);
}

if (!string.IsNullOrWhiteSpace(externalCache))
{
    cache = builder.AddConnectionString(AppConst.Cache);
}


#region containers
// 未提供外部连接串时，回退到本地容器资源，便于开发环境开箱即用。
var defaultName = isTesting ? "IAM_test" : "IAM_dev";
var devPassword = builder.AddParameter(
    "sql-password",
    value: aspireSetting.DevPassword,
    secret: true
);

if (database is null)
{
    database = builder
        .AddPostgres(name: "db", password: devPassword, port: aspireSetting.DbPort)
        .WithImageTag("18.1-alpine")
        .WithDataVolume()
        .AddDatabase(AppConst.Default, databaseName: defaultName);
}

if (cache is null)
{
    _ = aspireSetting.CacheType?.ToLowerInvariant() switch
    {
        "memory" => null,
        _ => cache = builder
            .AddRedis(AppConst.Cache, password: devPassword, port: aspireSetting.CachePort)
            .WithImageTag("8.2-alpine")
            .WithDataVolume()
            .WithPersistence(interval: TimeSpan.FromMinutes(5)),
    };
}

#endregion

var migration = builder.AddDotnetProject("MigrationService", "../Services/MigrationService/MigrationService.csproj");
var apiService = builder.AddDotnetProject("ApiService", "../Services/ApiService/ApiService.csproj")
    .WaitForCompletion(migration)
    .WithExternalHttpEndpoints();

if (builder.ExecutionContext.IsRunMode)
{
    var issuer = apiService.GetEndpoint("https");
    apiService.WithEnvironment("Authentication__Issuer", issuer);
    migration.WithEnvironment("Authentication__Issuer", issuer);
}
else
{
    apiService.WithEnvironment("Authentication__Issuer", publicOrigin);
    migration.WithEnvironment("Authentication__Issuer", publicOrigin);
}

var adminApp = builder.AddJavaScriptApp("AdminApp", "../ClientApp/WebApp", "start")
    .WithPnpm()
    .WithReference(apiService)
    .WaitFor(apiService);

apiService.PublishWithContainerFiles(adminApp, "./wwwroot");

if (builder.ExecutionContext.IsRunMode)
{
    var apiSampleService = builder.AddDotnetProject("ApiSampleService", "../Services/ApiSampleService/ApiSampleService.csproj")
        .WaitForCompletion(migration)
        .WithReference(apiService)
        .WithEnvironment("Authentication__OAuth__Authority", apiService.GetEndpoint("https"));

    builder.AddJavaScriptApp("FrontSampleService", "../Services/FrontSampleService", "start")
        .WithPnpm()
        .WithReference(apiService)
        .WithReference(apiSampleService)
        .WaitFor(apiService)
        .WaitFor(apiSampleService)
        .WithUrl("http://localhost:4201");

    adminApp.WithUrl("http://localhost:4200");

    if (database != null)
    {
        apiSampleService.WithReference(database);
    }
}

if (database != null)
{
    migration.WithReference(database).WaitFor(database);
    apiService.WithReference(database);
}
if (cache != null)
{
    migration.WithReference(cache).WaitFor(cache);
    apiService.WithReference(cache);
}

builder.Build().Run();
