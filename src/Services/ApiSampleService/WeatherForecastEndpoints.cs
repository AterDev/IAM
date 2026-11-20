using Microsoft.AspNetCore.Http.HttpResults;

namespace ApiSampleService;

internal static class WeatherForecastEndpoints
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/WeatherForecast").RequireAuthorization();
        group.MapGet("", Get);
        group.MapGet("/forecast/{days:int}", GetForecast);
    }

    public static Ok<WeatherForecastDto> Get(HttpContext context, ILoggerFactory loggerFactory)
    {
        var userId = context.User.FindFirst("sub")?.Value;
        loggerFactory.CreateLogger(typeof(WeatherForecastEndpoints).FullName!)
            .LogInformation("Weather forecast requested by user: {UserId}", userId);
        return TypedResults.Ok(new WeatherForecastDto("成功获取天气预报",
            context.User.Identity?.Name ?? userId, CreateForecast(5), DateTime.UtcNow));
    }

    public static Results<Ok<FutureWeatherForecastDto>, BadRequest<WeatherForecastErrorDto>> GetForecast(
        int days, HttpContext context)
    {
        if (days is < 1 or > 30)
        {
            return TypedResults.BadRequest(new WeatherForecastErrorDto("天数必须在1到30之间"));
        }

        return TypedResults.Ok(new FutureWeatherForecastDto($"未来{days}天的天气预报",
            context.User.Identity?.Name, CreateForecast(days)));
    }

    private static WeatherForecast[] CreateForecast(int days) =>
        Enumerable.Range(1, days).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)],
        }).ToArray();
}
