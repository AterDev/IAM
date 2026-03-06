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

// if you have exist resource, you can set connection string here, without create container
//var db = builder.AddConnectionString(AppConst.Default, "");
//kafka = builder.AddConnectionString("mq", "");
//es = builder.AddConnectionString("es", "");

#region containers
// 当前本地开发默认使用外部 PostgreSQL / Redis 资源，不再依赖 Docker 容器。
// 如需恢复容器模式，可取消以下代码注释，并在未提供 ConnectionStrings 时使用这些资源。
// var defaultName = "IAM_dev";
// var devPassword = builder.AddParameter(
//     "sql-password",
//     value: aspireSetting.DevPassword,
//     secret: true
// );
//
// _ = aspireSetting.DatabaseType?.ToLowerInvariant() switch
// {
//     "postgresql" => database = builder
//         .AddPostgres(name: "db", password: devPassword, port: aspireSetting.DbPort)
//         .WithImageTag("18.1-alpine")
//         .WithDataVolume()
//         .AddDatabase(AppConst.Default, databaseName: defaultName),
//     "sqlserver" => database = builder
//         .AddSqlServer(name: "db", password: devPassword, port: aspireSetting.DbPort)
//         .WithImageTag("2025-latest")
//         .WithDataVolume()
//         .AddDatabase(AppConst.Default, databaseName: defaultName),
//     _ => null,
// };
//
// _ = aspireSetting.CacheType?.ToLowerInvariant() switch
// {
//     "memory" => null,
//     _ => cache = builder
//         .AddRedis("Cache", password: devPassword, port: aspireSetting.CachePort)
//         .WithImageTag("8.2-alpine")
//         .WithDataVolume()
//         .WithPersistence(interval: TimeSpan.FromMinutes(5)),
// };

#endregion

var migration = builder.AddProject<Projects.MigrationService>("MigrationService");
var apiService = builder.AddProject<Projects.ApiService>("ApiService");

var apiSampleService = builder.AddProject<Projects.ApiSampleService>("ApiSampleService");

builder.AddJavaScriptApp("FrontSampleService", "../Services/FrontSampleService", "start")
    .WithPnpm()
    .WithReference(apiSampleService)
    .WithUrl("http://localhost:4201");

builder.AddJavaScriptApp("AdminApp", "../ClientApp/WebApp", "start")
    .WithPnpm()
    .WithReference(apiService)
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
