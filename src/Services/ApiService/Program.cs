using ApiService.Extension;
using ApiService.Serialization;
using ApiService.Browser;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// 共享基础服务:health check, service discovery, opentelemetry, http retry etc.
builder.AddServiceDefaults();

// 框架依赖服务:options, cache, dbContext
builder.AddFrameworkServices();

builder.Services.AddIamCacheSerializers();
builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_1;
    options.AddSchemaTransformer<ApiService.OpenApi.EnumSchemaTransformer>();
    options.CreateSchemaReferenceId = typeInfo =>
        Nullable.GetUnderlyingType(typeInfo.Type) is not null
        || Microsoft.AspNetCore.OpenApi.OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo) is null
            ? null
            : typeInfo.Type.FullName;
});
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, ApiService.ApiJsonSerializerContext.Default));

builder.Services.AddManagers();
builder.AddModules();
builder.Services.AddAntiforgery();
builder.Services.AddSingleton<BrowserFlowState>();

WebApplication app = builder.Build();

app.MapOpenApi();
app.MapEndpointGroups();
app.MapBrowserEndpoints();
app.UseIAMModServices();

await app.RunAsync();
