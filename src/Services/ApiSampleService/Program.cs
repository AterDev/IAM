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
        User = new
        {
            IsAuthenticated = user.Identity?.IsAuthenticated ?? false,
            Name = user.Identity?.Name,
            Subject = user.FindFirst("sub")?.Value,
            Email = user.FindFirst("email")?.Value,
            Claims = user.Claims.Select(c => new { c.Type, c.Value }).ToList(),
        },
        Timestamp = DateTime.UtcNow,
    });
}).RequireAuthorization()
  .WithName("GetProtected")
  .WithTags("Protected");

app.Run();
