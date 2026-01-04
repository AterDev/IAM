using AppHost;
using Perigon.AspNetCore.Constants;

var builder = DistributedApplication.CreateBuilder(args);
var aspireSetting = AppSettingsHelper.LoadAspireSettings(builder.Configuration);

IResourceBuilder<IResourceWithConnectionString>? database = null;
IResourceBuilder<IResourceWithConnectionString>? cache = null;

// if you have exist resource, you can set connection string here, without create container
//var db = builder.AddConnectionString(AppConst.Default, "");
//kafka = builder.AddConnectionString("mq", "");
//es = builder.AddConnectionString("es", "");

#region containers
var defaultName = "IAM_dev";

var devPassword = builder.AddParameter(
    "sql-password",
    value: aspireSetting.DevPassword,
    secret: true
);

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

_ = aspireSetting.CacheType?.ToLowerInvariant() switch
{
    "memory" => null,
    _ => cache = builder
        .AddRedis("Cache", password: devPassword, port: aspireSetting.CachePort)
        .WithImageTag("8.2-alpine")
        .WithDataVolume()
        .WithPersistence(interval: TimeSpan.FromMinutes(5)),
};

#endregion

devPassword.WithParentRelationship(database!);
var migration = builder.AddProject<Projects.MigrationService>("MigrationService");
var apiService = builder.AddProject<Projects.ApiService>("ApiService").WaitForCompletion(migration);

var sampleApi = builder.AddProject<Projects.SampleApi>("SampleApi");

builder.AddJavaScriptApp("SampleApp", "../../samples/frontend-angular", "start")
    .WithPnpm()
    .WithReference(sampleApi)
    .WithUrl("http://localhost:4201");

builder.AddJavaScriptApp("AdminApp", "../ClientApp/WebApp", "start")
    .WithPnpm()
    .WithReference(apiService)
    .WithUrl("http://localhost:4200");

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
