using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with JWT support
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IAM Sample API",
        Version = "v1",
        Description = "示例API项目，展示如何对接IAM授权系统"
    });

    // Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. 在下方输入访问令牌(从IAM获取的access_token)",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure CORS - read from configuration
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
    ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure JWT Bearer Authentication with IAM
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // IAM authority URL - the OpenID Connect discovery endpoint
        options.Authority = builder.Configuration["Authentication:Authority"] ?? "https://localhost:7001";
        
        // API resource name (audience) - must match the client registered in IAM
        options.Audience = builder.Configuration["Authentication:Audience"] ?? "ApiTest";
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = builder.Configuration.GetValue<bool>("Authentication:ValidateAudience", true),
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minutes clock skew
        };

        // For development - allow HTTP metadata endpoint
        options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Authentication:RequireHttpsMetadata", !builder.Environment.IsDevelopment());

        // Event handlers for debugging
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "Authentication failed");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogDebug("Token validated for user: {User}", context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "IAM Sample API v1");
        options.DocumentTitle = "IAM Sample API";
    });
}

app.UseHttpsRedirection();

// CORS must be before Authentication/Authorization
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Public endpoint - no authentication required
app.MapGet("/api/public", () => new 
{ 
    Message = "这是一个公开端点，无需认证",
    Timestamp = DateTime.UtcNow
})
    .WithName("GetPublic")
    .WithTags("Public");

// Protected endpoint - requires valid JWT token
app.MapGet("/api/protected", (HttpContext context) => 
{
    var user = context.User;
    return new 
    { 
        Message = "这是一个受保护端点，需要有效的JWT令牌",
        User = new
        {
            IsAuthenticated = user.Identity?.IsAuthenticated ?? false,
            Name = user.Identity?.Name,
            Subject = user.FindFirst("sub")?.Value,
            Email = user.FindFirst("email")?.Value,
            Claims = user.Claims.Select(c => new { c.Type, c.Value }).ToList()
        },
        Timestamp = DateTime.UtcNow
    };
})
    .RequireAuthorization()
    .WithName("GetProtected")
    .WithTags("Protected");

app.Run();
