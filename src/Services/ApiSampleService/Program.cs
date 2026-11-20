using ApiSampleService;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddMiddlewareServices();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, SampleJsonContext.Default));

var app = builder.Build();
app.UseMiddlewareServices();
app.MapDefaultEndpoints();
app.MapGet("/api/public", () => TypedResults.Ok(new SampleMessageDto("这是一个公开端点，无需认证", DateTime.UtcNow)))
    .WithName("GetPublic")
    .WithTags("Public");
app.MapGet("/api/protected", (HttpContext context) => TypedResults.Ok(
        new ProtectedMessageDto("这是一个受保护端点，需要有效的JWT令牌",
            context.User.Identity?.Name ?? context.User.FindFirst("sub")?.Value, DateTime.UtcNow)))
    .RequireAuthorization()
    .WithName("GetProtected")
    .WithTags("Protected");
WeatherForecastEndpoints.MapEndpoints(app);

app.Run();
