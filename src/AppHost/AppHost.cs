using AppHost;
using Microsoft.Extensions.Configuration;
using Perigon.AspNetCore.Constants;

var builder = DistributedApplication.CreateBuilder(args);
var aspireSetting = AppSettingsHelper.LoadAspireSettings(builder.Configuration);

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
var defaultName = "IAM_dev";
var devPassword = builder.AddParameter(
    "sql-password",
    value: aspireSetting.DevPassword,
    secret: true
);

if (database is null)
{
    _ = aspireSetting.DatabaseType?.ToLowerInvariant() switch
    {
        "postgresql" => database = builder
            .AddPostgres(name: "db", password: devPassword, port: aspireSetting.DbPort)
            .WithImageTag("18.1-alpine")
            .WithDataVolume()
            .AddDatabase(AppConst.Default, databaseName: defaultName),
        "sqlserver" => database = builder
            .AddSqlServer(name: "db", password: devPassword, port: aspireSetting.DbPort)
            .WithImageTag("2025-latest")
            .WithDataVolume()
            .AddDatabase(AppConst.Default, databaseName: defaultName),
        _ => null,
    };
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

var migration = builder.AddProject<Projects.MigrationService>("MigrationService");
var apiService = builder.AddProject<Projects.ApiService>("ApiService")
    .WaitForCompletion(migration);
var apiSampleService = builder.AddProject<Projects.ApiSampleService>("ApiSampleService")
    .WaitForCompletion(migration);

builder.AddJavaScriptApp("FrontSampleService", "../Services/FrontSampleService", "start")
    .WithPnpm()
    .WithReference(apiSampleService)
    .WaitFor(apiSampleService)
    .WithUrl("http://localhost:4201");

builder.AddJavaScriptApp("AdminApp", "../ClientApp/WebApp", "start")
    .WithPnpm()
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithUrl("http://localhost:4200");

if (database != null)
{
    migration.WithReference(database).WaitFor(database);
    apiService.WithReference(database);
    apiSampleService.WithReference(database);
}
if (cache != null)
{
    migration.WithReference(cache).WaitFor(cache);
    apiService.WithReference(cache);
}

builder.Build().Run();
