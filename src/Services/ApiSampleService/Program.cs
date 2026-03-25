using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddFrameworkServices();
builder.AddMiddlewareServices();

var app = builder.Build();

app.UseMiddlewareServices();

app.MapGet("/api/public", () => Results.Ok(new
{
    Message = "这是一个公开端点，无需认证",
    Timestamp = DateTime.UtcNow,
})).WithName("GetPublic")
  .WithTags("Public");

app.MapGet("/api/protected", (HttpContext context) =>
{
    var user = context.User;
    return Results.Ok(new
    {
        Message = "这是一个受保护端点，需要有效的JWT令牌",
    RequestedBy = user.Identity?.Name ?? user.FindFirst("sub")?.Value,
        Timestamp = DateTime.UtcNow,
    });
}).RequireAuthorization()
  .WithName("GetProtected")
  .WithTags("Protected");

app.Run();
