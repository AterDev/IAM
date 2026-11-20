using MigrationService;
using EntityFramework.AppDbFactory;
using IAMMod.Services;
using ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddSingleton<AppDbFactory>();
builder.Services.AddSingleton<InitHostService>();
builder.Services.AddHostedService<Worker>();

builder
    .Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

var host = builder.Build();
host.Run();
