using ApiService.Extension;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// 共享基础服务:health check, service discovery, opentelemetry, http retry etc.
builder.AddServiceDefaults();

// 框架依赖服务:options, cache, dbContext
builder.AddFrameworkServices();

builder.Services.AddManagers();
builder.AddModules();

WebApplication app = builder.Build();

app.UseIAMModServices();

app.Run();
