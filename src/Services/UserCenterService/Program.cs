 WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// 共享基础服务:health check, service discovery, opentelemetry, http retry etc.
builder.AddServiceDefaults();

// 框架依赖服务:options, cache, dbContext
builder.AddFrameworkServices();

// Web中间件服务:route, openapi, jwt, default cors, auth, rateLimiter etc.
builder.AddMiddlewareServices();

// this service's custom cors, auth, rateLimiter etc.

// add Managers, auto generate by source generator
builder.Services.AddManagers();

// add modules, auto generate by source generator
builder.AddModules();

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

// 使用中间件
app.UseMiddlewareServices();
app.Run();
